using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Classifieds;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Service.FileStorage;
using QLN.Common.Infrastructure.Subscriptions;
using QLN.Common.Infrastructure.Utilities;
using QLN.Common.Migrations.QLLog;
using static Dapr.Client.Autogen.Grpc.v1.Dapr;
using static QLN.Common.DTO_s.ClassifiedsIndex;

namespace QLN.Classified.MS.Service
{
    public class ClassifiedService : IClassifiedService
    {
        private readonly IWebHostEnvironment _env;
        private readonly Dapr.Client.DaprClient _dapr;

        private const string UnifiedStore = ConstantValues.StateStoreNames.UnifiedStore;
        private const string UnifiedIndexKey = ConstantValues.StateStoreNames.UnifiedIndexKey;
        private const string ItemsIndexKey = ConstantValues.StateStoreNames.ItemsIndexKey;
        private const string PrelovedIndexKey = ConstantValues.StateStoreNames.PrelovedIndexKey;
        private const string CollectiblesIndexKey = ConstantValues.StateStoreNames.CollectiblesIndexKey;
        private const string DealsIndexKey = ConstantValues.StateStoreNames.DealsIndexKey;
        private const string ItemsCategoryIndexKey = ConstantValues.StateStoreNames.ItemsCategoryIndexKey;
        private const string PrelovedCategoryIndexKey = ConstantValues.StateStoreNames.PrelovedCategoryIndexKey;
        private const string CollectiblesCategoryIndexKey = ConstantValues.StateStoreNames.CollectiblesCategoryIndexKey;
        private const string DealsCategoryIndexKey = ConstantValues.StateStoreNames.DealsCategoryIndexKey;
        private readonly QLClassifiedContext _context;
        private readonly QLCompanyContext _companyContext;
        private readonly QLSubscriptionContext _subscriptionContext;

        private readonly ILogger<ClassifiedService> _logger;
        private readonly string itemJsonPath = Path.Combine("ClassifiedMockData", "itemsAdsMock.json");
        private readonly string prelovedJsonPath = Path.Combine("ClassifiedMockData", "prelovedAdsMock.json");
        private readonly string CollectablesonPath = Path.Combine("ClassifiedMockData", "collectables.json");
        public ClassifiedService(Dapr.Client.DaprClient dapr, ILogger<ClassifiedService> logger, IWebHostEnvironment env, QLClassifiedContext context, QLCompanyContext companyContext, QLSubscriptionContext subscriptionContext)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _env = env;
            _context = context;
            _companyContext = companyContext;
            _subscriptionContext = subscriptionContext;
        }

        public async Task<bool> SaveSearchByVertical(SaveSearchRequestDto dto, string userId, CancellationToken cancellationToken = default)
        {
            try
            {                
                var actualUserId = !string.IsNullOrWhiteSpace(dto.UserId) ? dto.UserId : userId;

                if (string.IsNullOrWhiteSpace(actualUserId))
                {
                    _logger.LogError("UserId is required to save search");
                    return false;
                }

                bool isValidSubVertical = ValidateVerticalAndSubVertical(dto.Vertical, dto.SubVertical);
                if (!isValidSubVertical)
                {
                    _logger.LogError("Invalid SubVertical {SubVertical} for Vertical {Vertical}", dto.SubVertical, dto.Vertical);
                    return false;
                }

                var saveSearch = new SaveSearch
                {
                    Name = dto.Name,
                    SubVertical = dto.SubVertical,
                    Vertical = dto.Vertical,
                    SearchQuery = dto.SearchQuery,
                    CreatedAt = DateTime.UtcNow, 
                    UserId = actualUserId
                };

                _context.saveSearches.Add(saveSearch);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Search '{SearchName}' saved successfully for user {UserId}", dto.Name, actualUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save the search '{SearchName}' for user {UserId}", dto.Name, userId);
                throw new InvalidOperationException("Failed to save the search", ex);
            }
        }

        private bool ValidateVerticalAndSubVertical(Vertical vertical, SubVertical? subVertical)
        {
            if ((int)vertical == 3) 
            {
                return subVertical == SubVertical.Items ||
                       subVertical == SubVertical.Deals ||
                       subVertical == SubVertical.Stores ||
                       subVertical == SubVertical.Preloved ||
                       subVertical == SubVertical.Collectibles;
            }

            if ((int)vertical == 4) 
            {
                return subVertical == null;
            }
            return false;
        }

        public async Task<List<SavedSearchResponseDto>> GetSearches(string userId, Vertical vertical, SubVertical? subVertical = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("UserId is required.", nameof(userId));
                
                var query = _context.saveSearches
                    .Where(s => s.UserId == userId && s.Vertical == vertical);

                if (subVertical.HasValue)
                {
                    query = query.Where(s => s.SubVertical == subVertical.Value);
                }

                var savedSearches = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync(cancellationToken);

                var responseDto = savedSearches.Select(s => new SavedSearchResponseDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Vertical = s.Vertical,
                    SubVertical = s.SubVertical,
                    SearchQuery = s.SearchQuery,
                    CreatedAt = s.CreatedAt,
                    UserId = s.UserId
                }).ToList();

                //_logger.LogInformation("Retrieved {Count} saved searches from database for user {UserId}, vertical {Vertical}, subVertical {SubVertical}",
                //    responseDto.Count, userId, vertical, subVertical);

                return responseDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve saved searches from database for user {UserId}, vertical {Vertical}, subVertical {SubVertical}",
                    userId, vertical, subVertical);
                throw new InvalidOperationException("Failed to retrieve saved searches from database.", ex);
            }
        }

        public async Task<bool> SaveSearchById(SaveSearchRequestByIdDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Search request cannot be null.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Search name is required.", nameof(dto.Name));

            if (dto.SearchQuery == null)
                throw new ArgumentException("Search query details are required.", nameof(dto.SearchQuery));

            try
            {
                var key = $"search:{dto.UserId}";

                var existing = await _dapr.GetStateAsync<List<SavedSearchResponseDto>>(UnifiedStore, key)
                               ?? new List<SavedSearchResponseDto>();

                var newSearch = new SavedSearchResponseDto
                {
                    Id = Guid.NewGuid(),
                    UserId = dto.UserId,
                    Name = dto.Name,
                    CreatedAt = DateTime.UtcNow,
                    SearchQuery = dto.SearchQuery
                };

                existing.Insert(0, newSearch);

                if (existing.Count > 30)
                    existing = existing.Take(30).ToList();

                await _dapr.SaveStateAsync(UnifiedStore, key, existing);

                var confirm = await _dapr.GetStateAsync<List<SavedSearchResponseDto>>(UnifiedStore, key);
                if (confirm == null || !confirm.Any(x => x.Id == newSearch.Id))
                {
                    throw new InvalidOperationException("Failed to confirm that the search was saved.");
                }

                return true;
            }
            catch (DaprException dex)
            {
                Console.WriteLine($"Dapr error while saving search: {dex.Message}");
                throw new InvalidOperationException("Failed to save search due to Dapr error.", dex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while saving search: {ex.Message}");
                throw new InvalidOperationException("An unexpected error occurred while saving search.", ex);
            }
        }
     
        public Task<bool> SaveSearch(SaveSearchRequestDto dto, string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<AdCreatedResponseDto> CreateClassifiedItemsAd(
    Items dto,
    CancellationToken cancellationToken = default)
        {
            if (dto == null)
            {
                _logger.LogWarning("CreateClassifiedItemsAd called with null dto");
                throw new ArgumentNullException(nameof(dto));
            }

            if (dto.UserId == null)
            {
                _logger.LogWarning("CreateClassifiedItemsAd validation failed: UserId is null");
                throw new ArgumentException("UserId is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                _logger.LogWarning("CreateClassifiedItemsAd validation failed: Title is missing");
                throw new ArgumentException("Title is required.");
            }

            if (dto.Images == null || dto.Images.Count == 0)
            {
                _logger.LogWarning("CreateClassifiedItemsAd validation failed: Images are missing");
                throw new ArgumentException("Image URLs must be provided.");
            }

            try
            {
                _logger.LogInformation("Starting CreateClassifiedItemsAd for UserId={UserId}, Title='{Title}'", dto.UserId, dto.Title);

                dto.Status = AdStatus.Draft;
                dto.CreatedAt = DateTime.UtcNow;
                
                _logger.LogDebug("Adding Items ad to EF context...");
                _context.Item.Add(dto);

                _logger.LogDebug("Saving changes to database...");
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Database save completed. New AdId={AdId}", dto.Id);

                _logger.LogDebug("Indexing ad to Azure Search...");
                await IndexItemsToAzureSearch(dto, cancellationToken);
                _logger.LogInformation("Ad indexed to Azure Search successfully. AdId={AdId}", dto.Id);

                return new AdCreatedResponseDto
                {
                    AdId = dto.Id,
                    Title = dto.Title,                    
                    CreatedAt = DateTime.UtcNow,
                    Message = "Items Ad created successfully"
                };
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                _logger.LogWarning(ex, "Duplicate ad insert attempt for UserId={UserId}, Title='{Title}'", dto.UserId, dto.Title);
                throw new InvalidOperationException("Ad already exists. Conflict occurred during Items ad creation.", ex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed in CreateClassifiedItemsAd for UserId={UserId}", dto.UserId);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Operation error while creating classified Items ad for UserId={UserId}", dto.UserId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred during ad creation for UserId={UserId}, Title='{Title}'", dto.UserId, dto.Title);
                throw new InvalidOperationException(
                    "An unexpected error occurred while creating the Items ad. Please try again later.",
                    ex
                );
            }
        }

        public async Task<string> MigrateClassifiedItemsAd(Items dto, CancellationToken cancellationToken = default)
        {

            try
            {
                _logger.LogInformation("Starting MigrateClassifiedItemsAd for UserId={UserId}, Title='{Title}'", dto.UserId, dto.Title);

                _logger.LogDebug("Adding Items ad to EF context...");
                _context.Item.Add(dto);

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Database save completed. New AdId={AdId}", dto.Id);

                _logger.LogDebug("Indexing ad to Azure Search...");
                await IndexItemsToAzureSearch(dto, cancellationToken);
                _logger.LogInformation("Ad indexed to Azure Search successfully. AdId={AdId}", dto.Id);

                return $"Completed adding {dto.Id}";
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred during ad creation for UserId={UserId}, Title='{Title}'", dto.UserId, dto.Title);
                throw new InvalidOperationException(
                    "An unexpected error occurred while creating the Items ad. Please try again later.",
                    ex
                );
            }
        }


        public async Task<AdCreatedResponseDto> RefreshClassifiedItemsAd(
     SubVertical subVertical,
     long adId,
     string userId,
     Guid subscriptionId,
     CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "RefreshClassifiedItemsAd called. SubVertical: {SubVertical}, AdId: {AdId}, UserId: {UserId}",
                subVertical, adId, userId);

            try
            {
                subscriptionId = Guid.Parse("5a024f96-7414-4473-80b8-f5d70297e262");
                //var subcription = await _subscriptionContext.Subscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionid,cancellationToken);
                string? adTitle;

                object? adItem = subVertical switch
                {
                    SubVertical.Items => await _context.Item.FirstOrDefaultAsync(i => i.Id == adId && i.IsActive, cancellationToken),
                    SubVertical.Preloved => await _context.Preloved.FirstOrDefaultAsync(p => p.Id == adId && p.IsActive, cancellationToken),
                    SubVertical.Collectibles => await _context.Collectible.FirstOrDefaultAsync(c => c.Id == adId && c.IsActive, cancellationToken),
                    SubVertical.Deals => await _context.Deal.FirstOrDefaultAsync(d => d.Id == adId && d.IsActive, cancellationToken),
                    _ => throw new InvalidOperationException($"Invalid SubVertical: {subVertical}")
                };

                if (adItem == null)
                {
                    _logger.LogError("Ad with id {AdId} not found in {SubVertical}.", adId, subVertical);
                    throw new KeyNotFoundException($"Ad with id {adId} not found.");
                }

                _logger.LogDebug("Updating refresh fields for ad type: {Type}", adItem.GetType().Name);

                switch (adItem)
                {
                    case Items itemAd:
                        itemAd.CreatedAt = DateTime.UtcNow;
                        itemAd.LastRefreshedOn = DateTime.UtcNow.AddHours(72);
                        itemAd.UpdatedAt = DateTime.UtcNow;
                        itemAd.UpdatedBy = userId;
                        itemAd.IsRefreshed = true;
                        adTitle = itemAd.Title;
                        break;

                    case Preloveds prelovedAd:
                        prelovedAd.CreatedAt = DateTime.UtcNow;
                        prelovedAd.LastRefreshedOn = DateTime.UtcNow.AddHours(72);
                        prelovedAd.UpdatedAt = DateTime.UtcNow;
                        prelovedAd.UpdatedBy = userId;
                        prelovedAd.IsRefreshed = true;
                        adTitle = prelovedAd.Title;
                        break;

                    case Collectibles collectiblesAd:
                        collectiblesAd.CreatedAt = DateTime.UtcNow;
                        //collectiblesAd.LastRefreshedOn = DateTime.UtcNow.AddHours(72);
                        collectiblesAd.UpdatedAt = DateTime.UtcNow;
                        collectiblesAd.UpdatedBy = userId;
                        adTitle = collectiblesAd.Title;
                        break;

                    case Deals dealsAd:
                        dealsAd.CreatedAt = DateTime.UtcNow;
                       // dealsAd.las = DateTime.UtcNow.AddHours(72);
                        dealsAd.UpdatedAt = DateTime.UtcNow;
                        dealsAd.UpdatedBy = userId;
                        adTitle = dealsAd.Offertitle;
                        break;

                    default:
                        _logger.LogError("Unsupported ad type: {Type}", adItem.GetType().Name);
                        throw new InvalidOperationException($"Unsupported ad type: {adItem.GetType().Name}");
                }

                _logger.LogDebug("Saving changes to database for AdId: {AdId}", adId);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Indexing refreshed ad {AdId} in Azure Search for {SubVertical}", adId, subVertical);
                switch (subVertical)
                {
                    case SubVertical.Items:
                        await IndexItemsToAzureSearch((Items)adItem, cancellationToken);
                        break;

                    case SubVertical.Preloved:
                        await IndexPrelovedToAzureSearch((Preloveds)adItem, cancellationToken);
                        break;

                    case SubVertical.Collectibles:
                        await IndexCollectiblesToAzureSearch((Collectibles)adItem, cancellationToken);
                        break;

                    case SubVertical.Deals:
                        await IndexDealsToAzureSearch((Deals)adItem, cancellationToken);
                        break;
                }

                _logger.LogInformation("Ad {AdId} successfully refreshed and indexed.", adId);


                _logger.LogInformation("Ad {AdId} successfully refreshed.", adId);

                return new AdCreatedResponseDto
                {
                   // Title = adTitle,
                    CreatedAt = DateTime.UtcNow,
                    AdId = adId,
                    Message = "Ad successfully refreshed."
                };
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is KeyNotFoundException ||
                ex is InvalidOperationException)
            {
                _logger.LogError(ex, "Known error occurred while refreshing ad with AdId: {AdId}", adId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred while refreshing ad with AdId: {AdId}", adId);
                throw new InvalidOperationException("Failed to refresh the ad due to an unexpected error.", ex);
            }
        }

        public async Task<AdCreatedResponseDto> CreateClassifiedPrelovedAd(Preloveds dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.UserId)) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (dto.Images == null || dto.Images.Count == 0)
                throw new ArgumentException("Image URLs must be provided.");
            if (string.IsNullOrWhiteSpace(dto.AuthenticityCertificateUrl))
                throw new ArgumentException("Certificate URL must be provided.");            

            try
            {                
                dto.Status = AdStatus.Draft;
                dto.CreatedAt = DateTime.UtcNow;

                _context.Preloved.Add(dto);
                await _context.SaveChangesAsync(cancellationToken);

                await IndexPrelovedToAzureSearch(dto, cancellationToken);

                return new AdCreatedResponseDto
                {
                    AdId = dto.Id,
                    Title = dto.Title,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Preloved created successfully"
                };
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                _logger.LogWarning(ex, "Duplicate ad insert attempt.");
                throw new InvalidOperationException("Ad already exists. Conflict occurred during Preloved ad creation.", ex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed in CreateClassifiedPrelovedAd");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Operation error while creating classified Preloved ad.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred during ad creation.");
                throw new InvalidOperationException("An unexpected error occurred while creating the Preloved ad. Please try again later.", ex);
            }
        }

        public async Task<AdCreatedResponseDto> CreateClassifiedCollectiblesAd(Collectibles dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (dto.Images == null || dto.Images.Count == 0) throw new ArgumentException("Image URLs must be provided.");
            if (string.IsNullOrWhiteSpace(dto.AuthenticityCertificateUrl) && dto.HasAuthenticityCertificate)
                throw new ArgumentException("Certificate URL must be provided.");         

            try
            {
              
                dto.Status = AdStatus.Draft;
                dto.CreatedAt = DateTime.UtcNow;

                _context.Collectible.Add(dto);
                await _context.SaveChangesAsync(cancellationToken);

                await IndexCollectiblesToAzureSearch(dto, cancellationToken);

                return new AdCreatedResponseDto
                {
                    AdId = dto.Id,
                    Title = dto.Title,
                    CreatedAt = dto.CreatedAt,
                    Message = "Collectibles created successfully"
                };
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                _logger.LogWarning(ex, "Duplicate ad insert attempt.");
                throw new InvalidOperationException("Ad already exists. Conflict occurred during Collectibles ad creation.", ex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed in CreateClassifiedCollectiblesAd");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Operation error while creating classified Collectibles ad.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred during Collectibles ad creation.");
                throw new InvalidOperationException("An unexpected error occurred while creating the Collectibles ad. Please try again later.", ex);
            }
        }

        public async Task<string> MigrateClassifiedCollectiblesAd(Collectibles dto, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting MigrateClassifiedItemsAd for UserId={UserId}, Title='{Title}'", dto.UserId, dto.Title);

                _logger.LogDebug("Adding Items ad to EF context...");
                _context.Collectible.Add(dto);

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Database save completed. New AdId={AdId}", dto.Id);

                _logger.LogDebug("Indexing ad to Azure Search...");
                
                await IndexCollectiblesToAzureSearch(dto, cancellationToken);

                _logger.LogInformation("Ad indexed to Azure Search successfully. AdId={AdId}", dto.Id);
                return $"Completed adding {dto.Id}";

            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred during Collectibles ad creation.");
                throw new InvalidOperationException("An unexpected error occurred while creating the Collectibles ad. Please try again later.", ex);
            }
        }

        public async Task<AdCreatedResponseDto> CreateClassifiedDealsAd(Deals dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.UserId)) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Offertitle)) throw new ArgumentException("Title is required.");
            if (string.IsNullOrWhiteSpace(dto.FlyerFileUrl)) throw new ArgumentException("Flyer URL must be provided.");
            if (dto.CoverImage == null || !dto.CoverImage.Any()) throw new ArgumentException("At least one image is required.");

          
            try
            {
                var company = await _companyContext.Companies
                    .FirstOrDefaultAsync(c => c.UserId == dto.UserId && c.SubVertical == SubVertical.Deals && c.IsActive, cancellationToken);

                if (company == null)
                    throw new InvalidOperationException($"No company found for user ID: {dto.UserId}");

                dto.BusinessName = company.CompanyName;
                dto.BusinessType = company.CompanyType.ToString();
                dto.BranchNames = company.BranchLocations != null && company.BranchLocations.Any()
                    ? string.Join(", ", company.BranchLocations)
                    : string.Empty;
                if (string.IsNullOrWhiteSpace(dto.ContactNumber))
                    dto.ContactNumber = company.PhoneNumber;
                if (string.IsNullOrWhiteSpace(dto.WhatsappNumber))
                    dto.WhatsappNumber = company.WhatsAppNumber;
                var socialLinks = new List<string>();
                if (!string.IsNullOrWhiteSpace(company.FacebookUrl))
                    socialLinks.Add(company.FacebookUrl);
                if (!string.IsNullOrWhiteSpace(company.InstagramUrl))
                    socialLinks.Add(company.InstagramUrl);

                dto.SocialMediaLinks = socialLinks.Any() ? string.Join(", ", socialLinks) : null;
                dto.Status = AdStatus.Draft; 
                dto.CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt;
                dto.CompanyLogo = company.CompanyLogo;


                _context.Deal.Add(dto);
                await _context.SaveChangesAsync(cancellationToken);

                await IndexDealsToAzureSearch(dto, cancellationToken);

                return new AdCreatedResponseDto
                {
                    AdId = dto.Id,
                    Title = dto.Offertitle,
                    CreatedAt = dto.CreatedAt,
                    Message = "Deals Ad created successfully"
                };

            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                _logger.LogWarning(ex, "Duplicate ad insert attempt.");
                throw new InvalidOperationException("Ad already exists. Conflict occurred during Deals ad creation.", ex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed in CreateClassifiedDealsAd");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Operation error while creating classified Deals ad.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred during Deals ad creation.");
                throw new InvalidOperationException("An unexpected error occurred while creating the Deals ad. Please try again later.", ex);
            }
        }
      
        public async Task<DeleteAdResponseDto> DeleteClassifiedAd(SubVertical subVertical, long adId, string userId, CancellationToken cancellationToken = default)
        {
            if (adId <= 0)
                throw new ArgumentException("Ad ID must be a valid positive number.", nameof(adId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.", nameof(userId));

            try
            {
                switch (subVertical)
                {
                    case SubVertical.Items:
                        {
                            var entity = await _context.Item
                                .FirstOrDefaultAsync(x => x.Id == adId && x.IsActive == true, cancellationToken);

                            if (entity == null)
                                throw new KeyNotFoundException($"Items ad with ID {adId} not found.");

                            if (!string.Equals(entity.UserId, userId, StringComparison.OrdinalIgnoreCase))
                                throw new UnauthorizedAccessException("You are not authorized to delete this ad.");

                            entity.IsActive = false;
                            entity.UpdatedAt = DateTime.UtcNow;

                            await _context.SaveChangesAsync(cancellationToken);

                            return new DeleteAdResponseDto { Message = "Ad soft-deleted successfully" };
                        }

                    case SubVertical.Preloved:
                        {
                            var entity = await _context.Preloved
                                .FirstOrDefaultAsync(x => x.Id == adId && x.IsActive == true, cancellationToken);

                            if (entity == null)
                                throw new KeyNotFoundException($"Preloved ad with ID {adId} not found.");

                            if (!string.Equals(entity.UserId, userId, StringComparison.OrdinalIgnoreCase))
                                throw new UnauthorizedAccessException("You are not authorized to delete this ad.");

                            entity.IsActive = false;
                            entity.UpdatedAt = DateTime.UtcNow;

                            await _context.SaveChangesAsync(cancellationToken);

                            return new DeleteAdResponseDto { Message = "Ad soft-deleted successfully" };
                        }

                    case SubVertical.Collectibles:
                        {
                            var entity = await _context.Collectible
                                .FirstOrDefaultAsync(x => x.Id == adId && x.IsActive == true, cancellationToken);

                            if (entity == null)
                                throw new KeyNotFoundException($"Collectibles ad with ID {adId} not found.");

                            if (!string.Equals(entity.UserId, userId, StringComparison.OrdinalIgnoreCase))
                                throw new UnauthorizedAccessException("You are not authorized to delete this ad.");

                            entity.IsActive = false;
                            entity.UpdatedAt = DateTime.UtcNow;

                            await _context.SaveChangesAsync(cancellationToken);

                            return new DeleteAdResponseDto { Message = "Ad soft-deleted successfully" };
                        }

                    case SubVertical.Deals:
                        {
                            var entity = await _context.Deal
                                .FirstOrDefaultAsync(x => x.Id == adId && x.IsActive == true, cancellationToken);

                            if (entity == null)
                                throw new KeyNotFoundException($"Deals ad with ID {adId} not found.");

                            if (!string.Equals(entity.UserId, userId, StringComparison.OrdinalIgnoreCase))
                                throw new UnauthorizedAccessException("You are not authorized to delete this ad.");

                            entity.IsActive = false;
                            entity.UpdatedAt = DateTime.UtcNow;

                            await _context.SaveChangesAsync(cancellationToken);

                            return new DeleteAdResponseDto { Message = "Ad soft-deleted successfully" };
                        }

                    default:
                        throw new InvalidOperationException($"Unsupported subvertical: {subVertical}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting classified ad with ID: {AdId}", adId);
                throw new InvalidOperationException("Failed to delete Classified Ad.", ex);
            }
        }

        private static string ExtractBlobName(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            try
            {
                return new Uri(url).Segments.LastOrDefault()?.Trim('/');
            }
            catch
            {
                return null;
            }
        }

        public async Task<Items> GetItemAdById(long adId, CancellationToken cancellationToken = default)
        {
            if (adId <= 0)
                throw new ArgumentException("Ad ID must not be empty.", nameof(adId));
            try
            {
                var adItem = await _context.Item.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == adId && i.IsActive == true, cancellationToken);

                if (adItem == null)
                {
                    _logger.LogWarning("Ad ID {AdId} not found in database or IsActive.", adId);
                    throw new KeyNotFoundException($"Ad with ID {adId} does not exist.");
                }

                return adItem;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching classified item details by adId: {AdId}", adId);
                throw new InvalidOperationException("Failed to fetch classified item ad by ID.", ex);
            }
        }

        public async Task<Items> GetItemAdBySlug(string slug, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug must not be null or empty.", nameof(slug));

            try
            {
                var adItem = await _context.Item.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Slug == slug && i.IsActive == true, cancellationToken);

                if (adItem == null)
                {
                    _logger.LogWarning("Slug '{Slug}' not found in database or IsActive.", slug);
                    throw new KeyNotFoundException($"Ad with Slug '{slug}' does not exist.");
                }

                return adItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching classified item details by slug: {Slug}", slug);
                throw new InvalidOperationException("Failed to fetch classified item ad by Slug.", ex);
            }
        }

        public async Task<Preloveds> GetPrelovedAdBySlug(string slug, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug must not be null or empty.", nameof(slug));

            try
            {
                var adItem = await _context.Preloved.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Slug == slug && i.IsActive == true, cancellationToken);

                if (adItem == null)
                {
                    _logger.LogWarning("Slug '{Slug}' not found in database or IsActive.", slug);
                    throw new KeyNotFoundException($"Ad with Slug '{slug}' does not exist.");
                }

                return adItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching classified preloved details by slug: {Slug}", slug);
                throw new InvalidOperationException("Failed to fetch classified preloved ad by Slug.", ex);
            }
        }

        public async Task<List<Items>> GetAllItemsAdByUser(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID must not be empty.", nameof(userId));

            try
            {
                var ads = await _context.Item.AsNoTracking()
                    .Where(i => i.UserId == userId && i.IsActive)                                                                                 
                    .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)      
                    .ToListAsync(cancellationToken);

                if (ads.Count == 0)
                    _logger.LogInformation("No active Items ads found for user {UserId}.", userId);

                return ads;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error while fetching Items ads for user: {UserId}", userId);
                throw new InvalidOperationException("Failed to fetch Items ads by user.", ex);
            }
        }

        public async Task<List<Preloveds>> GetAllPrelovedAdByUser(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID must not be empty.", nameof(userId));

            try
            {
                var ads = await _context.Preloved.AsNoTracking()
                    .Where(i => i.UserId == userId && i.IsActive)
                    .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
                    .ToListAsync(cancellationToken);

                if (ads.Count == 0)
                    _logger.LogInformation("No active Preloved ads found for user {UserId}.", userId);

                return ads;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching Preloved ads for user: {UserId}", userId);
                throw new InvalidOperationException("Failed to fetch Preloved ads by user.", ex);
            }
        }

        public async Task<List<Collectibles>> GetAllCollectiblesAdByUser(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID must not be empty.", nameof(userId));

            try
            {
                var ads = await _context.Collectible.AsNoTracking()
                    .Where(i => i.UserId == userId && i.IsActive)
                    .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
                    .ToListAsync(cancellationToken);

                if (ads.Count == 0)
                    _logger.LogInformation("No active Collectibles ads found for user {UserId}.", userId);

                return ads;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching Collectibles ads for user: {UserId}", userId);
                throw new InvalidOperationException("Failed to fetch Collectibles ads by user.", ex);
            }
        }

        public async Task<Collectibles> GetCollectiblesAdBySlug(string slug, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug must not be null or empty.", nameof(slug));

            try
            {
                var adItem = await _context.Collectible.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Slug == slug && i.IsActive == true, cancellationToken);

                if (adItem == null)
                {
                    _logger.LogWarning("Slug '{Slug}' not found in database or IsActive.", slug);
                    throw new KeyNotFoundException($"Ad with Slug '{slug}' does not exist.");
                }

                return adItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching classified collectibles details by slug: {Slug}", slug);
                throw new InvalidOperationException("Failed to fetch classified preloved ad by Slug.", ex);
            }
        }

        public async Task<Deals> GetDealsAdBySlug(string slug, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug must not be null or empty.", nameof(slug));

            try
            {
                var adItem = await _context.Deal.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Slug == slug && i.IsActive == true, cancellationToken);

                if (adItem == null)
                {
                    _logger.LogWarning("Slug '{Slug}' not found in database or IsActive.", slug);
                    throw new KeyNotFoundException($"Ad with Slug '{slug}' does not exist.");
                }

                return adItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching classified deals details by slug: {Slug}", slug);
                throw new InvalidOperationException("Failed to fetch classified deals ad by Slug.", ex);
            }
        }

        public async Task<List<Deals>> GetAllDealsAdByUser(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID must not be empty.", nameof(userId));

            try
            {
                var ads = await _context.Deal.AsNoTracking()
                    .Where(i => i.UserId == userId && i.IsActive)
                    .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
                    .ToListAsync(cancellationToken);

                if (ads.Count == 0)
                    _logger.LogInformation("No active Deals ads found for user {UserId}.", userId);

                return ads;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching Deals ads for user: {UserId}", userId);
                throw new InvalidOperationException("Failed to fetch Deals ads by user.", ex);
            }
        }

        public async Task<Preloveds> GetPrelovedAdById(long adId, CancellationToken cancellationToken = default)
        {
            if (adId <= 0)
                throw new ArgumentException("Ad ID must not be empty.", nameof(adId));

            try
            {
                var adPreloved = await _context.Preloved.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == adId, cancellationToken);

                if (adPreloved == null)
                {
                    _logger.LogWarning("Ad ID {AdId} not found in DB or is inactive.", adId);
                    throw new KeyNotFoundException($"Ad with ID {adId} does not exist.");
                }

                return adPreloved;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching classified preloved ad by ID: {AdId}", adId);
                throw new InvalidOperationException($"Failed to fetch classified preloved ad by ID {adId}.", ex);
            }
        }

        public async Task<Deals> GetDealsAdById(long adId, CancellationToken cancellationToken = default)
        {
            if (adId <= 0)
                throw new ArgumentException("Ad ID must not be empty.", nameof(adId));
            try
            {
                var adDeals = await _context.Deal.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == adId && d.IsActive == true, cancellationToken);

                if (adDeals == null)
                {
                    _logger.LogWarning("Ad ID {AdId} not found in database or is inactive.", adId);
                    throw new KeyNotFoundException($"Ad with ID {adId} does not exist.");
                }

                return adDeals;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching classified deals details by adId: {AdId}", adId);
                throw new InvalidOperationException("Failed to fetch classified deals ad by ID.", ex);
            }
        }

        public async Task<Collectibles> GetCollectiblesAdById(long adId, CancellationToken cancellationToken = default)
        {
            if (adId <= 0)
                throw new ArgumentException("Ad ID must not be empty.", nameof(adId));
            try
            {
                var adCollectibles = await _context.Collectible.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == adId && c.IsActive == true, cancellationToken);

                if (adCollectibles == null)
                {
                    _logger.LogWarning("Ad ID {AdId} not found in DB or marked as inactive.", adId);
                    throw new KeyNotFoundException($"Ad with ID {adId} does not exist.");
                }

                return adCollectibles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching classified collectibles details by adId: {AdId}", adId);
                throw new InvalidOperationException("Failed to fetch classified collectibles ad by ID.", ex);
            }
        }

        public async Task<Guid> CreateCategory(CategoryDtos dto, CancellationToken cancellationToken)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Vertical)) throw new ArgumentException("Vertical must be specified.");
            try
            {
                var category = new Categories
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    ParentId = dto.ParentId ?? Guid.Empty,
                    Fields = dto.Fields
                };

                var key = $"category-{category.Id}";

                var indexKey = dto.Vertical.Trim().ToLowerInvariant() switch
                {
                    "items" => ItemsCategoryIndexKey,
                    "preloved" => PrelovedCategoryIndexKey,
                    "collectibles" => CollectiblesCategoryIndexKey,
                    "deals" => DealsCategoryIndexKey,
                    _ => throw new ArgumentException($"Unsupported vertical: {dto.Vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, indexKey) ?? new();
                index.Add(key);

                await _dapr.SaveStateAsync(UnifiedStore, key, category);
                await _dapr.SaveStateAsync(UnifiedStore, indexKey, index);

                return category.Id;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create category internally", ex);
            }
        }

        public async Task<List<Categories>> GetChildCategories(string vertical, Guid parentId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical must be provided.");

            try
            {
                var indexKey = vertical.Trim().ToLowerInvariant() switch
                {
                    "items" => ItemsCategoryIndexKey,
                    "preloved" => PrelovedCategoryIndexKey,
                    "collectibles" => CollectiblesCategoryIndexKey,
                    "deals" => DealsCategoryIndexKey,
                    _ => throw new ArgumentException($"Unsupported vertical: {vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, indexKey) ?? new();
                var result = new List<Categories>();

                foreach (var key in index)
                {
                    try
                    {
                        var cat = await _dapr.GetStateAsync<Categories>(UnifiedStore, key);
                        if (cat != null && cat.ParentId == parentId)
                        {
                            result.Add(cat);
                        }
                    }
                    catch (Exception catEx)
                    {
                        _logger.LogError(catEx, "Failed to retrieve or process category from key: {Key}", key);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve child categories", ex);
            }
        }

        public async Task<CategoryTreeDto?> GetCategoryTree(string vertical, Guid categoryId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical must be specified.");

            try
            {
                var indexKey = vertical.Trim().ToLowerInvariant() switch
                {
                    "items" => ItemsCategoryIndexKey,
                    "preloved" => PrelovedCategoryIndexKey,
                    "collectibles" => CollectiblesCategoryIndexKey,
                    "deals" => DealsCategoryIndexKey,
                    _ => throw new ArgumentException($"Unsupported vertical: {vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, indexKey) ?? new();

                var rootKey = $"category-{categoryId}";
                var root = await _dapr.GetStateAsync<Categories>(UnifiedStore, rootKey);

                if (root == null)
                {
                    _logger.LogWarning("Root category not found for ID: {CategoryId}", categoryId);
                    return null;
                }

                var rootNode = new CategoryTreeDto
                {
                    Id = root.Id,
                    Name = root.Name,
                    Fields = root.Fields ?? new(),
                    Children = new()
                };

                foreach (var key in index)
                {
                    try
                    {
                        var child = await _dapr.GetStateAsync<Categories>(UnifiedStore, key);
                        if (child != null && child.ParentId == categoryId)
                        {
                            var childNode = await GetCategoryTree(vertical, child.Id, cancellationToken);
                            if (childNode != null)
                            {
                                rootNode.Children.Add(childNode);
                            }
                        }
                    }
                    catch (Exception childEx)
                    {
                        _logger.LogError(childEx, "Error processing child category from key: {Key}", key);
                    }
                }

                return rootNode;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to build category tree for categoryId {categoryId}", ex);
            }
        }

        public async Task DeleteCategoryTree(string vertical, Guid categoryId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical must be specified.");
            try
            {
                var indexKey = vertical.Trim().ToLowerInvariant() switch
                {
                    "items" => ItemsCategoryIndexKey,
                    "preloved" => PrelovedCategoryIndexKey,
                    "collectibles" => CollectiblesCategoryIndexKey,
                    "deals" => DealsCategoryIndexKey,
                    _ => throw new ArgumentException($"Unsupported vertical: {vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, indexKey) ?? new();
                var keysToDelete = new List<string>();

                async Task TraverseAndCollectKeys(Guid parentId)
                {
                    try
                    {
                        var parentKey = $"category-{parentId}";
                        var category = await _dapr.GetStateAsync<Categories>(UnifiedStore, parentKey);
                        if (category != null)
                        {
                            keysToDelete.Add(parentKey);
                        }

                        foreach (var key in index.ToList())
                        {
                            try
                            {
                                var child = await _dapr.GetStateAsync<Categories>(UnifiedStore, key);
                                if (child != null && child.ParentId == parentId)
                                {
                                    await TraverseAndCollectKeys(child.Id);
                                }
                            }
                            catch (Exception childEx)
                            {
                                _logger.LogError(childEx, "Failed to fetch or traverse child category for key: {Key}", key);
                            }
                        }
                    }
                    catch (Exception traverseEx)
                    {
                        _logger.LogError(traverseEx, "Failed while traversing parentId: {ParentId}", parentId);
                        throw;
                    }
                }

                await TraverseAndCollectKeys(categoryId);

                foreach (var key in keysToDelete)
                {
                    try
                    {
                        await _dapr.DeleteStateAsync(UnifiedStore, key);
                        index.Remove(key);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogError(deleteEx, "Failed to delete key: {Key}", key);
                    }
                }

                await _dapr.SaveStateAsync(UnifiedStore, indexKey, index);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete category tree for ID {categoryId}", ex);
            }
        }

        public async Task<List<CategoryTreeDto>> GetAllCategoryTrees(string vertical, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical must be specified.");

            try
            {
                var indexKey = vertical.Trim().ToLowerInvariant() switch
                {
                    "items" => ItemsCategoryIndexKey,
                    "preloved" => PrelovedCategoryIndexKey,
                    "collectibles" => CollectiblesCategoryIndexKey,
                    "deals" => DealsCategoryIndexKey,
                    _ => throw new ArgumentException($"Unsupported vertical: {vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, indexKey) ?? new List<string>();
                var result = new List<CategoryTreeDto>();


                if (!index.Any())
                {
                    _logger.LogInformation("No category index found for vertical: {Vertical}", vertical);
                    return new List<CategoryTreeDto>();
                }


                foreach (var key in index)
                {
                    try
                    {
                        var category = await _dapr.GetStateAsync<Categories>(UnifiedStore, key);
                        if (category != null && category.ParentId == Guid.Empty)
                        {
                            var tree = await GetCategoryTree(vertical, category.Id, cancellationToken);
                            if (tree != null)
                            {
                                result.Add(tree);
                            }
                        }
                    }
                    catch (Exception catEx)
                    {
                        _logger.LogError(catEx, "Error processing category tree for key: {Key}", key);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve all category trees", ex);
            }
        }

        public async Task<List<CategoryField>> GetFiltersByMainCategoryAsync(string vertical, Guid mainCategoryId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical must be specified.");

            if (mainCategoryId == Guid.Empty)
                throw new ArgumentException("Main category ID must not be empty.");

            try
            {
                var allTrees = await GetAllCategoryTrees(vertical, cancellationToken)
                    .ConfigureAwait(false);

                var root = allTrees.FirstOrDefault(t => t.Id == mainCategoryId);
                if (root == null)
                    return new List<CategoryField>();

                var collected = new List<CategoryField>();
                void Collect(CategoryTreeDto node)
                {
                    if (node.Fields?.Any() == true)
                        collected.AddRange(node.Fields);

                    foreach (var child in node.Children)
                        Collect(child);
                }
                Collect(root);

                var merged = collected
                    .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(g =>
                    {
                        var exemplar = g.First();
                        var allOptions = g
                            .Where(f => f.Options != null)
                            .SelectMany(f => f.Options!)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        return new CategoryField
                        {
                            Name = exemplar.Name,
                            Type = exemplar.Type,
                            Options = allOptions
                        };
                    })
                    .ToList();

                return merged;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve filters from the category tree.", ex);
            }
        }
        public async Task<AdUpdatedResponseDto> UpdateClassifiedItemsAd(Items dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.UpdatedBy == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            try
            {
                var existingAd = await GetItemAdById(dto.Id, cancellationToken);
                if (existingAd == null)
                    throw new KeyNotFoundException($"Ad with ID {dto.Id} does not exist.");

                if (dto.SubVertical != SubVertical.Items)
                    throw new InvalidOperationException("This service only supports updating ads under the 'Items' vertical.");

                AdUpdateHelper.ApplySelectiveUpdates(existingAd, dto);

                existingAd.FeaturedExpiryDate = existingAd.FeaturedExpiryDate;
                existingAd.IsFeatured = existingAd.IsFeatured;
                existingAd.PromotedExpiryDate = existingAd.PromotedExpiryDate;
                existingAd.IsPromoted = existingAd.IsPromoted;
                existingAd.LastRefreshedOn = existingAd.LastRefreshedOn;
                existingAd.IsRefreshed = existingAd.IsRefreshed;
                existingAd.Title = existingAd.Title;
                existingAd.Description = existingAd.Description;
                existingAd.Price = existingAd.Price;
                existingAd.PriceType = existingAd.PriceType;
                existingAd.Location = existingAd.Location;
                existingAd.Status = existingAd.Status;
                existingAd.Location = dto.Location;
                existingAd.Latitude = dto.Latitude;
                existingAd.Longitude = dto.Longitude;
                existingAd.ContactNumber = dto.ContactNumber;
                existingAd.WhatsAppNumber = dto.WhatsAppNumber;
                existingAd.ContactEmail = dto.ContactEmail;
                existingAd.StreetNumber = dto.StreetNumber;
                existingAd.BuildingNumber = dto.BuildingNumber;
                existingAd.zone = dto.zone;
                existingAd.Images = dto.Images;
                existingAd.Attributes = dto.Attributes;
                existingAd.IsActive = true;
                existingAd.CreatedAt = existingAd.CreatedAt;
                existingAd.CreatedBy = existingAd.CreatedBy;
                existingAd.UpdatedAt = DateTime.UtcNow;

                _context.Item.Update(existingAd);
                await _context.SaveChangesAsync(cancellationToken);

                await IndexItemsToAzureSearch(existingAd, cancellationToken);


                return new AdUpdatedResponseDto
                {
                    AdId = existingAd.Id,
                    Title = existingAd.Title,
                    UpdatedAt = existingAd.UpdatedAt ?? DateTime.UtcNow,
                    Message = "Items Ad updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Items Ad.");
                throw new InvalidOperationException("Failed to update Items ad.", ex);
            }
        }

        public async Task<AdUpdatedResponseDto> UpdateClassifiedPrelovedAd(Preloveds dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.UpdatedBy == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            try
            {
                var existingAd = await GetPrelovedAdById(dto.Id, cancellationToken);
                if (existingAd == null)
                    throw new KeyNotFoundException($"Ad with ID {dto.Id} does not exist.");

                if (dto.SubVertical != SubVertical.Preloved)
                    throw new InvalidOperationException("This service only supports updating ads under the 'Preloved' vertical.");

                AdUpdateHelper.ApplySelectiveUpdates(existingAd, dto);

                existingAd.HasAuthenticityCertificate = dto.HasAuthenticityCertificate;
                existingAd.AuthenticityCertificateUrl = dto.AuthenticityCertificateUrl;
                existingAd.Inclusion = dto.Inclusion;
                existingAd.IsFeatured = existingAd.IsFeatured;
                existingAd.FeaturedExpiryDate = existingAd.FeaturedExpiryDate;
                existingAd.IsPromoted = existingAd.IsPromoted;
                existingAd.PromotedExpiryDate = existingAd.PromotedExpiryDate;
                existingAd.LastRefreshedOn = existingAd.LastRefreshedOn;
                existingAd.SubscriptionId = existingAd.SubscriptionId;
                existingAd.IsRefreshed = existingAd.IsRefreshed;
                existingAd.Title = dto.Title;
                existingAd.Description = dto.Description;
                existingAd.Price = dto.Price;
                existingAd.PriceType = dto.PriceType;
                existingAd.Location = dto.Location;
                existingAd.Latitude = dto.Latitude;
                existingAd.Longitude = dto.Longitude;
                existingAd.ContactNumber = dto.ContactNumber;
                existingAd.WhatsAppNumber = dto.WhatsAppNumber;
                existingAd.ContactEmail = dto.ContactEmail;
                existingAd.StreetNumber = dto.StreetNumber;
                existingAd.BuildingNumber = dto.BuildingNumber;
                existingAd.zone = dto.zone;
                existingAd.Images = dto.Images;
                existingAd.Attributes = dto.Attributes;
                existingAd.UpdatedAt = DateTime.UtcNow;

                _context.Preloved.Update(existingAd);
                await _context.SaveChangesAsync(cancellationToken);

                await IndexPrelovedToAzureSearch(existingAd, cancellationToken);

                return new AdUpdatedResponseDto
                {
                    AdId = existingAd.Id,
                    Title = existingAd.Title,
                    UpdatedAt = DateTime.UtcNow,
                    Message = "Preloved Ad updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Preloved Ad.");
                throw new InvalidOperationException("Failed to update Preloved ad.", ex);
            }
        }

        public async Task<AdUpdatedResponseDto> UpdateClassifiedCollectiblesAd(Collectibles dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.UpdatedBy == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            try
            {
                var existingAd = await GetCollectiblesAdById(dto.Id, cancellationToken);
                if (existingAd == null)
                    throw new KeyNotFoundException($"Ad with ID {dto.Id} does not exist.");

                if (dto.SubVertical != SubVertical.Collectibles)
                    throw new InvalidOperationException("This service only supports updating ads under the 'Collectibles' vertical.");


                AdUpdateHelper.ApplySelectiveUpdates(existingAd, dto);

                existingAd.AuthenticityCertificateName = dto.AuthenticityCertificateName;
                existingAd.AuthenticityCertificateUrl = dto.AuthenticityCertificateUrl;
                existingAd.HasAuthenticityCertificate = dto.HasAuthenticityCertificate;
                existingAd.HasAuthenticityCertificate = dto.HasAuthenticityCertificate;
                existingAd.IsActive = true;
                existingAd.IsFeatured = existingAd.IsFeatured;
                existingAd.FeaturedExpiryDate = existingAd.FeaturedExpiryDate;
                existingAd.IsPromoted = existingAd.IsPromoted;
                existingAd.PromotedExpiryDate = existingAd.PromotedExpiryDate;
                existingAd.SubscriptionId = existingAd.SubscriptionId;
                existingAd.HasWarranty = dto.HasWarranty;
                existingAd.IsPromoted = existingAd.IsPromoted;
                existingAd.IsHandmade = dto.IsHandmade;
                existingAd.YearOrEra = dto.YearOrEra;
                existingAd.Title = dto.Title;
                existingAd.Description = dto.Description;
                existingAd.Price = dto.Price;
                existingAd.PriceType = dto.PriceType;
                existingAd.Status = existingAd.Status;
                existingAd.Location = dto.Location;
                existingAd.Latitude = dto.Latitude;
                existingAd.Longitude = dto.Longitude;
                existingAd.ContactNumber = dto.ContactNumber;
                existingAd.WhatsAppNumber = dto.WhatsAppNumber;
                existingAd.ContactEmail = dto.ContactEmail;
                existingAd.StreetNumber = dto.StreetNumber;
                existingAd.BuildingNumber = dto.BuildingNumber;
                existingAd.zone = dto.zone;
                existingAd.Images = dto.Images;
                existingAd.Attributes = dto.Attributes;
                existingAd.IsActive = true;
                existingAd.CreatedAt = existingAd.CreatedAt;
                existingAd.CreatedBy = existingAd.CreatedBy;

                existingAd.UpdatedAt = DateTime.UtcNow;

                _context.Collectible.Update(existingAd);
                await _context.SaveChangesAsync(cancellationToken);

                await IndexCollectiblesToAzureSearch(existingAd, cancellationToken);

                return new AdUpdatedResponseDto
                {
                    AdId = existingAd.Id,
                    Title = existingAd.Title,
                    UpdatedAt = DateTime.UtcNow,
                    Message = "Collectibles Ad updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Collectibles Ad.");
                throw new InvalidOperationException("Failed to update Collectibles ad.", ex);
            }
        }

        public async Task<AdUpdatedResponseDto> UpdateClassifiedDealsAd(Deals dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.UpdatedBy == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Offertitle)) throw new ArgumentException("Title is required.");

            try
            {
                var existingAd = await GetDealsAdById(dto.Id, cancellationToken);
                if (existingAd == null)
                    throw new KeyNotFoundException($"Ad with ID {dto.Id} does not exist.");

                //AdUpdateHelper.ApplySelectiveUpdates(existingAd, dto);

                var company = await _companyContext.Companies
                    .FirstOrDefaultAsync(c => c.UserId == dto.UserId && c.IsActive == true, cancellationToken);

                if (company != null)
                {
                    if (string.IsNullOrWhiteSpace(existingAd.BusinessName))
                        existingAd.BusinessName = company.CompanyName;

                    if (string.IsNullOrWhiteSpace(existingAd.BusinessType))
                        existingAd.BusinessType = company.CompanyType.ToString();

                    if (string.IsNullOrWhiteSpace(existingAd.BranchNames))
                        existingAd.BranchNames = company.BranchLocations != null && company.BranchLocations.Any()
                            ? string.Join(", ", company.BranchLocations)
                            : string.Empty;

                    if (string.IsNullOrWhiteSpace(existingAd.ContactNumber))
                        existingAd.ContactNumber = company.PhoneNumber;

                    if (string.IsNullOrWhiteSpace(existingAd.WhatsappNumber))
                        existingAd.WhatsappNumber = company.WhatsAppNumber;

                    if (string.IsNullOrWhiteSpace(existingAd.CompanyLogo))
                        existingAd.CompanyLogo = company.CompanyLogo;

                        if (string.IsNullOrWhiteSpace(existingAd.SocialMediaLinks))
                    {
                        var socialLinks = new List<string>();
                        if (!string.IsNullOrWhiteSpace(company.FacebookUrl))
                            socialLinks.Add(company.FacebookUrl);
                        if (!string.IsNullOrWhiteSpace(company.InstagramUrl))
                            socialLinks.Add(company.InstagramUrl);

                        existingAd.SocialMediaLinks = socialLinks.Any() ? string.Join(", ", socialLinks) : null;
                    }
                }

                existingAd.UpdatedAt = DateTime.UtcNow;
                existingAd.IsFeatured = false;
                existingAd.FeaturedExpiryDate = null;
                existingAd.IsPromoted = false;
                existingAd.PromotedExpiryDate = null;
                existingAd.Status = AdStatus.Draft;
                existingAd.SubscriptionId = existingAd.SubscriptionId;
                existingAd.Offertitle = dto.Offertitle;
                existingAd.Description = dto.Description;
                existingAd.StartDate = dto.StartDate;
                existingAd.EndDate = dto.EndDate;
                existingAd.ExpiryDate = dto.ExpiryDate;
                existingAd.FlyerFileUrl = dto.FlyerFileUrl;
                existingAd.DataFeedUrl = dto.DataFeedUrl;
                existingAd.ContactNumber = dto.ContactNumber;
                existingAd.WhatsappNumber = dto.WhatsappNumber;
                existingAd.WebsiteUrl = dto.WebsiteUrl;
                existingAd.XMLlink = dto.XMLlink;
                existingAd.CoverImage = dto.CoverImage;
                existingAd.CreatedAt = existingAd.CreatedAt;
                existingAd.CreatedBy = existingAd.CreatedBy;
                existingAd.UpdatedBy = dto.UpdatedBy;
                existingAd.IsActive = true;
                existingAd.UpdatedAt = existingAd.UpdatedAt;


                _context.Deal.Update(existingAd);
                await _context.SaveChangesAsync(cancellationToken);

                await IndexDealsToAzureSearch(existingAd, cancellationToken);

                return new AdUpdatedResponseDto
                {
                    AdId = existingAd.Id,
                    Title = existingAd.Offertitle,
                    UpdatedAt = existingAd.UpdatedAt ?? DateTime.UtcNow,
                    Message = "Deals Ad updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Deals Ad.");
                throw new InvalidOperationException("Failed to update Deals ad.", ex);
            }
        }


        public async Task<PaginatedAdResponseDto> GetFilteredAds(SubVertical subVertical, bool? isPublished, int page, int pageSize, string? search, string userId, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID must not be empty.", nameof(userId));
                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 10;

                _logger.LogInformation(
                    "GetFilteredAds (DB) | SubVertical: {SubVertical}, IsPublished: {IsPublished}, Page: {Page}, PageSize: {PageSize}, UserId: {UserId}, Search: {Search}",
                    subVertical, isPublished, page, pageSize, userId, search);


                string? searchLower = string.IsNullOrWhiteSpace(search) ? null : search.ToLowerInvariant();
                
                IQueryable<Items> ItemsFilter(IQueryable<Items> q)
                    => q.Where(i => i.UserId == userId && i.IsActive)
                        .Where(i => searchLower == null || (i.Title ?? "").ToLower().Contains(searchLower))
                        .Where(i => !isPublished.HasValue
                            || (isPublished.Value
                                ? (i.Status == AdStatus.Published || i.Status == AdStatus.Approved)
                                : (i.Status != AdStatus.Published && i.Status != AdStatus.Approved)))
                        .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt);

                IQueryable<Preloveds> PrelovedFilter(IQueryable<Preloveds> q)
                    => q.Where(p => p.UserId == userId && p.IsActive)
                        .Where(p => searchLower == null || (p.Title ?? "").ToLower().Contains(searchLower))
                        .Where(p => !isPublished.HasValue
                            || (isPublished.Value
                                ? (p.Status == AdStatus.Published || p.Status == AdStatus.Approved)
                                : (p.Status != AdStatus.Published && p.Status != AdStatus.Approved)))
                        .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt);

                IQueryable<Collectibles> CollectiblesFilter(IQueryable<Collectibles> q)
                    => q.Where(c => c.UserId == userId && c.IsActive)
                        .Where(c => searchLower == null || (c.Title ?? "").ToLower().Contains(searchLower))
                        .Where(c => !isPublished.HasValue
                            || (isPublished.Value
                                ? (c.Status == AdStatus.Published || c.Status == AdStatus.Approved)
                                : (c.Status != AdStatus.Published && c.Status != AdStatus.Approved)))
                        .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt);

                IQueryable<Deals> DealsFilter(IQueryable<Deals> q)
                    => q.Where(d => d.UserId == userId && d.IsActive)
                        .Where(d => searchLower == null || (d.Offertitle ?? "").ToLower().Contains(searchLower))
                        .Where(d => !isPublished.HasValue
                            || (isPublished.Value
                                ? (d.Status == AdStatus.Published || d.Status == AdStatus.Approved)
                                : (d.Status != AdStatus.Published && d.Status != AdStatus.Approved)))
                        .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt);

                List<object> pageItems;
                int total;

                switch (subVertical)
                {
                    case SubVertical.Items:
                        {
                            var baseQ = ItemsFilter(_context.Item.AsNoTracking());
                            total = await baseQ.CountAsync(cancellationToken);
                            var list = await baseQ.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
                            pageItems = list.Cast<object>().ToList();
                            break;
                        }
                    case SubVertical.Preloved:
                        {
                            var baseQ = PrelovedFilter(_context.Preloved.AsNoTracking());
                            total = await baseQ.CountAsync(cancellationToken);
                            var list = await baseQ.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
                            pageItems = list.Cast<object>().ToList();
                            break;
                        }
                    case SubVertical.Collectibles:
                        {
                            var baseQ = CollectiblesFilter(_context.Collectible.AsNoTracking());
                            total = await baseQ.CountAsync(cancellationToken);
                            var list = await baseQ.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
                            pageItems = list.Cast<object>().ToList();
                            break;
                        }
                    case SubVertical.Deals:
                        {
                            var baseQ = DealsFilter(_context.Deal.AsNoTracking());
                            total = await baseQ.CountAsync(cancellationToken);
                            var list = await baseQ.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
                            pageItems = list.Cast<object>().ToList();
                            break;
                        }
                    default:
                        throw new ArgumentException($"Unsupported subVertical: {subVertical}", nameof(subVertical));
                }

                return new PaginatedAdResponseDto
                {
                    Total = total,
                    Items = pageItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFilteredAds | SubVertical: {SubVertical}, UserId: {UserId}", subVertical, userId);
                throw new InvalidOperationException("Failed to fetch filtered ads.", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkUpdateAdPublishStatusAsync(
    int subVertical,
    string userId,
    List<long> adIds,
    bool isPublished,
    CancellationToken cancellationToken = default)
        {
            try
            {
                var targetStatus = isPublished ? AdStatus.Published : AdStatus.Unpublished;

                IQueryable<ClassifiedBase> query = subVertical switch
                {
                    (int)SubVertical.Items => _context.Item.Cast<ClassifiedBase>(),
                    (int)SubVertical.Deals => _context.Deal.Cast<ClassifiedBase>(),
                    (int)SubVertical.Preloved => _context.Preloved.Cast<ClassifiedBase>(),
                    (int)SubVertical.Collectibles => _context.Collectible.Cast<ClassifiedBase>(),
                    _ => throw new ArgumentException("Invalid sub-vertical.")
                };

                // Get ads from DB that match the provided IDs
                var ads = await query
                    .Where(a => adIds.Contains(a.Id))
                    .ToListAsync(cancellationToken);

                var failedAds = new List<long>();

                // Validate ads before updating
                foreach (var ad in ads)
                {
                    if (ad.UserId != userId)
                    {
                        _logger.LogWarning("Ad {AdId} failed validation: UserId mismatch. Expected {Expected}, found {Actual}.",
                            ad.Id, userId, ad.UserId);
                        failedAds.Add(ad.Id);
                    }
                    else if (ad.Status == targetStatus)
                    {
                        _logger.LogWarning("Ad {AdId} failed validation: Already has target status {Status}.",
                            ad.Id, ad.Status);
                        failedAds.Add(ad.Id);
                    }
                    else if (!ad.IsActive)
                    {
                        _logger.LogWarning("Ad {AdId} failed validation: Ad is not active.",
                            ad.Id);
                        failedAds.Add(ad.Id);
                    }
                }

                // Check for ads not found in DB
                var notFound = adIds.Except(ads.Select(a => a.Id)).ToList();
                foreach (var id in notFound)
                {
                    _logger.LogWarning("Ad {AdId} failed validation: Not found in database.", id);
                }
                failedAds.AddRange(notFound);

                if (failedAds.Any())
                {
                    _logger.LogInformation("Bulk update failed for {FailedCount} out of {TotalCount} ads.",
                        failedAds.Distinct().Count(), adIds.Count);

                    return new BulkAdActionResponse
                    {
                        SuccessCount = 0,
                        FailedAdIds = failedAds.Distinct().ToList(),
                        Message = "Some ads failed validation."
                    };
                }

                // Update ads
                foreach (var ad in ads)
                {
                    ad.Status = targetStatus;
                    ad.CreatedAt = DateTime.UtcNow;
                    _logger.LogInformation("Ad {AdId} status updated to {Status}.", ad.Id, targetStatus);
                }

                await _context.SaveChangesAsync(cancellationToken);

                foreach (var ad in ads)
                {
                    switch ((SubVertical)subVertical)
                    {
                        case SubVertical.Items:
                            var item = await _context.Item.FirstOrDefaultAsync(i => i.Id == ad.Id, cancellationToken);
                            if (item != null)
                                await IndexItemsToAzureSearch(item, cancellationToken);
                            break;

                        case SubVertical.Deals:
                            var deal = await _context.Deal.FirstOrDefaultAsync(d => d.Id == ad.Id, cancellationToken);
                            if (deal != null)
                                await IndexDealsToAzureSearch(deal, cancellationToken);
                            break;

                        case SubVertical.Preloved:
                            var preloved = await _context.Preloved.FirstOrDefaultAsync(p => p.Id == ad.Id, cancellationToken);
                            if (preloved != null)
                                await IndexPrelovedToAzureSearch(preloved, cancellationToken);
                            break;

                        case SubVertical.Collectibles:
                            var collectible = await _context.Collectible.FirstOrDefaultAsync(c => c.Id == ad.Id, cancellationToken);
                            if (collectible != null)
                                await IndexCollectiblesToAzureSearch(collectible, cancellationToken);
                            break;
                    }
                }


                _logger.LogInformation("{Count} ad(s) successfully {Action}.",
                    ads.Count, isPublished ? "published" : "unpublished");

                return new BulkAdActionResponse
                {
                    SuccessCount = ads.Count,
                    FailedAdIds = new(),
                    Message = $"{ads.Count} ad(s) {(isPublished ? "published" : "unpublished")} successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk publish/unpublish failed.");
                throw new InvalidOperationException("An error occurred during bulk update.", ex);
            }
        }



        #region Private Methods
        private async Task IndexItemsToAzureSearch(Items dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ClassifiedsItemsIndex
            {
                Id = dto.Id.ToString(),
                SubVertical = SubVertical.Items.ToString(),
                AdType = dto.AdType.ToString(),
                Title = dto.Title,
                Slug = dto.Slug,
                Description = dto.Description,
                CategoryId = dto.CategoryId.ToString(),
                L1CategoryId = dto.L1CategoryId.ToString(),
                L2CategoryId = dto.L2CategoryId.ToString(),
                Category = dto.Category,
                L1Category = dto.L1Category,
                L2Category = dto.L2Category,
                Brand = dto.Brand,
                Model = dto.Model,
                Color = dto.Color,
                Condition = dto.Condition,
                SubscriptionId = dto.SubscriptionId.ToString(),
                Price = (double)dto.Price,
                PriceType = dto.PriceType,
                Location = dto.Location,
                Longitude = (double)dto.Longitude,
                Latitude = (double)dto.Latitude,
                IsFeatured = dto.IsFeatured,
                IsPromoted = dto.IsPromoted,
                Status = dto.Status.ToString(),
                FeaturedExpiryDate = dto.FeaturedExpiryDate,
                PromotedExpiryDate = dto.PromotedExpiryDate,
                UserId = dto.UserId,
                LastRefreshedOn = dto.LastRefreshedOn,
                BuildingNumber = dto.BuildingNumber,
                ContactEmail = dto.ContactEmail,
                ContactNumber = dto.ContactNumber,
                ContactNumberCountryCode = dto.ContactNumberCountryCode,
                StreetNumber = dto.StreetNumber,
                WhatsAppNumber = dto.WhatsAppNumber,
                WhatsappNumberCountryCode = dto.WhatsappNumberCountryCode,
                Zone = dto.zone,
                IsRefreshed = dto.IsRefreshed,
                PublishedDate = dto.PublishedDate,
                ExpiryDate = dto.ExpiryDate,
                UserName = dto.UserName,
                AttributesJson = dto.Attributes != null ? JsonSerializer.Serialize(dto.Attributes) : null,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                UpdatedBy = dto.UpdatedBy,
                Images = dto.Images.Select(i => new ImageInfo
                {
                    Url = i.Url,
                    Order = i.Order
                }).ToList()
            };
            var indexRequest = new CommonIndexRequest
            {
                IndexName = ConstantValues.IndexNames.ClassifiedsItemsIndex,
                ClassifiedsItem = indexDoc
            };
            if (indexRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ClassifiedsItemsIndex,
                    UpsertRequest = indexRequest
                };

                await _dapr.PublishEventAsync(
                    pubsubName: ConstantValues.PubSubName,
                    topicName: ConstantValues.PubSubTopics.IndexUpdates,
                    data: message,
                    cancellationToken: cancellationToken
                );
            }
        }
        private async Task IndexPrelovedToAzureSearch(Preloveds dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ClassifiedsPrelovedIndex
            {
                Id = dto.Id.ToString(),
                SubscriptionId = dto.SubscriptionId.ToString(),
                SubVertical = SubVertical.Preloved.ToString(),
                AdType = dto.AdType.ToString(),
                Slug = dto.Slug,
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                PriceType = dto.PriceType,
                CategoryId = dto.CategoryId.ToString(),
                Category = dto.Category,
                L1CategoryId = dto.L1CategoryId.ToString(),
                L1Category = dto.L1Category,
                L2CategoryId = dto.L2CategoryId.ToString(),
                L2Category = dto.L2Category,
                Location = dto.Location,
                CreatedAt = dto.CreatedAt,
                PublishedDate = dto.PublishedDate,
                ExpiryDate = dto.ExpiryDate,
                Status = dto.Status.ToString(),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Zone = dto.zone,
                WhatsappNumberCountryCode = dto.WhatsappNumberCountryCode,
                WhatsAppNumber = dto.WhatsAppNumber,
                StreetNumber = dto.StreetNumber,
                LastRefreshedOn = dto.LastRefreshedOn,
                BuildingNumber = dto.BuildingNumber,
                ContactEmail = dto.ContactEmail,
                ContactNumberCountryCode = dto.ContactNumberCountryCode,
                ContactNumber = dto.ContactNumber,
                UserId = dto.UserId,
                AuthenticityCertificateUrl = dto.AuthenticityCertificateUrl,
                Brand = dto.Brand,
                Color = dto.Color,
                Condition = dto.Condition,
                CreatedBy = dto.CreatedBy,
                HasAuthenticityCertificate = dto.HasAuthenticityCertificate,
                Inclusion = dto.Inclusion,
                Model = dto.Model,
                UserName = dto.UserName,
                IsActive = true,
                UpdatedAt = dto.UpdatedAt,
                UpdatedBy = dto.UpdatedBy,
                Images = dto.Images.Select(i => new ImageInfo
                {
                    Url = i.Url,
                    Order = i.Order
                }).ToList(),
                AttributesJson = dto.Attributes != null ? JsonSerializer.Serialize(dto.Attributes) : null,

                IsFeatured = dto.IsFeatured,
                FeaturedExpiryDate = dto.FeaturedExpiryDate,
                IsPromoted = dto.IsPromoted,
                PromotedExpiryDate = dto.PromotedExpiryDate,
                IsRefreshed = dto.IsRefreshed
            };
            var indexRequest = new CommonIndexRequest
            {
                IndexName = ConstantValues.IndexNames.ClassifiedsPrelovedIndex,
                ClassifiedsPrelovedItem = indexDoc
            };

            if (indexRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ClassifiedsPrelovedIndex,
                    UpsertRequest = indexRequest
                };

                await _dapr.PublishEventAsync(
                    pubsubName: ConstantValues.PubSubName,
                    topicName: ConstantValues.PubSubTopics.IndexUpdates,
                    data: message,
                    cancellationToken: cancellationToken
                );
            }
        }
        private async Task IndexCollectiblesToAzureSearch(Collectibles dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ClassifiedsCollectiblesIndex
            {
                Id = dto.Id.ToString(),
                SubVertical = SubVertical.Collectibles.ToString(),
                SubscriptionId = dto.SubscriptionId.ToString(),
                AdType = dto.AdType.ToString(),
                Title = dto.Title,
                Description = dto.Description,
                Slug = dto.Slug,
                Price = dto.Price,
                PriceType = dto.PriceType,
                CategoryId = dto.CategoryId.ToString(),
                Category = dto.Category,
                L1CategoryId = dto.L1CategoryId.ToString(),
                L1Category = dto.L1Category,
                L2CategoryId = dto.L2CategoryId.ToString(),
                L2Category = dto.L2Category,
                Location = dto.Location,
                CreatedAt = dto.CreatedAt,
                PublishedDate = dto.PublishedDate,
                ExpiryDate = dto.ExpiryDate,
                Status = dto.Status.ToString(),
                Latitude = dto.Latitude,
                Color = dto.Color,
                ContactNumber = dto.ContactNumber,
                BuildingNumber = dto.BuildingNumber,
                ContactNumberCountryCode = dto.ContactNumberCountryCode,
                ContactEmail = dto.ContactEmail,
                StreetNumber = dto.StreetNumber,
                Model = dto.Model,
                IsHandmade = dto.IsHandmade,
                HasWarranty = dto.HasWarranty,
                Condition = dto.Condition,
                Brand = dto.Brand,
                AuthenticityCertificateUrl = dto.AuthenticityCertificateUrl,
                CreatedBy = dto.CreatedBy,
                AuthenticityCertificateName = dto.AuthenticityCertificateName,
                HasAuthenticityCertificate = dto.HasAuthenticityCertificate,
                WhatsAppNumber = dto.WhatsAppNumber,
                WhatsappNumberCountryCode = dto.WhatsappNumberCountryCode,
                YearOrEra = dto.YearOrEra,
                Zone = dto.zone,
                Longitude = dto.Longitude,
                UserId = dto.UserId,
                UserName = dto.UserName,
                IsActive = true,
                UpdatedAt = dto.UpdatedAt,
                UpdatedBy = dto.UpdatedBy,
                Images = dto.Images.Select(i => new ImageInfo
                {
                    Url = i.Url,
                    Order = i.Order
                }).ToList(),
                AttributesJson = dto.Attributes != null ? JsonSerializer.Serialize(dto.Attributes) : null,

                IsFeatured = dto.IsFeatured,
                FeaturedExpiryDate = dto.FeaturedExpiryDate,
                IsPromoted = dto.IsPromoted,
                PromotedExpiryDate = dto.PromotedExpiryDate

            };
            var indexRequest = new CommonIndexRequest
            {
                IndexName = ConstantValues.IndexNames.ClassifiedsCollectiblesIndex,
                ClassifiedsCollectiblesItem = indexDoc
            };
            if (indexRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ClassifiedsCollectiblesIndex,
                    UpsertRequest = indexRequest
                };

                await _dapr.PublishEventAsync(
                    pubsubName: ConstantValues.PubSubName,
                    topicName: ConstantValues.PubSubTopics.IndexUpdates,
                    data: message,
                    cancellationToken: cancellationToken
                );
            }
        }
        private async Task IndexDealsToAzureSearch(Deals dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ClassifiedsDealsIndex
            {
                Id = dto.Id.ToString(),                
                UserId = dto.UserId,
                BusinessName = dto.BusinessName,
                BranchNames = dto.BranchNames,
                BusinessType = dto.BusinessType,
                Slug = dto.Slug,
                offertitle = dto.Offertitle,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                FlyerFileUrl = dto.FlyerFileUrl,
                DataFeedUrl = dto.DataFeedUrl,
                ContactNumber = dto.ContactNumber,
                WhatsappNumber = dto.WhatsappNumber,
                WebsiteUrl = dto.WebsiteUrl,
                SocialMediaLinks = dto.SocialMediaLinks,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedAt = dto.CreatedAt,
                XMLlink = dto.XMLlink,                
                SubscriptionId = dto.SubscriptionId,
                UpdatedAt = dto.UpdatedAt,
                UpdatedBy = dto.UpdatedBy,
                ExpiryDate = dto.ExpiryDate,
                CoverImage = dto.CoverImage,
                PromotedExpiryDate = dto.PromotedExpiryDate,
                IsPromoted = dto.IsPromoted,
                FeaturedExpiryDate = dto.FeaturedExpiryDate,
                IsFeatured = dto.IsFeatured,
            };

            var indexRequest = new CommonIndexRequest
            {
                IndexName = ConstantValues.IndexNames.ClassifiedsDealsIndex,
                ClassifiedsDealsItem = indexDoc
            };
            if (indexRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ClassifiedsDealsIndex,
                    UpsertRequest = indexRequest
                };

                await _dapr.PublishEventAsync(
                    pubsubName: ConstantValues.PubSubName,
                    topicName: ConstantValues.PubSubTopics.IndexUpdates,
                    data: message,
                    cancellationToken: cancellationToken
                );
            }
        }
        #endregion
               
        public async Task<string> FeatureClassifiedAd(ClassifiedsPromoteDto dto, string userId,Guid subscriptionId, CancellationToken cancellationToken)
        {
            try
            {
                 var subscriptionid = Guid.Parse("5a024f96-7414-4473-80b8-f5d70297e262");
               // var subcription = await _subscriptionContext.Subscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);
                if (dto is null) throw new ArgumentNullException(nameof(dto));
                if (dto.AdId <= 0) throw new ArgumentException("AdId must be a positive number.", nameof(dto.AdId));
                if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId must not be empty.", nameof(userId));

                _logger.LogInformation("FeatureClassifiedAd | SubVertical: {SubVertical}, AdId: {AdId}, UserId: {UserId}",
                    dto.SubVertical, dto.AdId, userId);

                object? adItem = null;

                switch (dto.SubVertical)
                {
                    case SubVertical.Items:
                        {
                            var ad = await _context.Item
                                .FirstOrDefaultAsync(x => x.Id == dto.AdId && x.IsActive, cancellationToken);
                            if (ad == null)
                                throw new KeyNotFoundException($"Items ad {dto.AdId} not found or inactive.");

                            if (ad.IsFeatured == true)
                                throw new ConflictException("This ad is already featured.");

                            ad.IsFeatured = true;
                            ad.UpdatedAt = DateTime.UtcNow;
                            ad.FeaturedExpiryDate = DateTime.UtcNow.AddDays(30);
                            adItem = ad;
                            await _context.SaveChangesAsync(cancellationToken);
                            break;
                        }

                    case SubVertical.Preloved:
                        {                            
                            var ad = await _context.Preloved
                                .FirstOrDefaultAsync(x => x.Id == dto.AdId && x.IsActive, cancellationToken);
                            if (ad == null)
                                throw new KeyNotFoundException($"Preloved ad {dto.AdId} not found or inactive.");
                           
                            if (ad.IsFeatured == true)
                                throw new ConflictException("This ad is already featured.");

                            ad.IsFeatured = true;
                            ad.FeaturedExpiryDate = DateTime.UtcNow.AddDays(30);
                            ad.UpdatedAt = DateTime.UtcNow;
                            adItem = ad;
                            await _context.SaveChangesAsync(cancellationToken);
                            break;
                        }

                    case SubVertical.Collectibles:
                        {
                            var ad = await _context.Collectible
                                .FirstOrDefaultAsync(x => x.Id == dto.AdId && x.IsActive, cancellationToken);
                            if (ad == null)
                                throw new KeyNotFoundException($"Collectibles ad {dto.AdId} not found or inactive.");
                         
                            if (ad.IsFeatured == true)
                                throw new ConflictException("This ad is already featured.");

                            ad.IsFeatured = true;
                            ad.FeaturedExpiryDate = DateTime.UtcNow.AddDays(30);
                            ad.UpdatedAt = DateTime.UtcNow;
                            adItem = ad;
                            await _context.SaveChangesAsync(cancellationToken);
                            break;
                        }

                    case SubVertical.Deals:
                        {
                            var ad = await _context.Deal
                                .FirstOrDefaultAsync(x => x.Id == dto.AdId && x.IsActive, cancellationToken);
                            if (ad == null)
                                throw new KeyNotFoundException($"Deals ad {dto.AdId} not found or inactive.");
                           
                            if (ad.IsFeatured == true)
                                throw new ConflictException("This ad is already featured.");

                            ad.IsFeatured = true;
                            ad.FeaturedExpiryDate = DateTime.UtcNow.AddDays(30);
                            ad.UpdatedAt = DateTime.UtcNow;
                            adItem = ad;
                            await _context.SaveChangesAsync(cancellationToken);
                            break;
                        }

                    default:
                        throw new InvalidOperationException($"Invalid SubVertical: {dto.SubVertical}");
                }

                if (adItem != null)
                {
                    _logger.LogDebug("Indexing featured ad {AdId} in Azure Search for {SubVertical}", dto.AdId, dto.SubVertical);
                    switch (dto.SubVertical)
                    {
                        case SubVertical.Items:
                            await IndexItemsToAzureSearch((Items)adItem, cancellationToken);
                            break;

                        case SubVertical.Preloved:
                            await IndexPrelovedToAzureSearch((Preloveds)adItem, cancellationToken);
                            break;

                        case SubVertical.Collectibles:
                            await IndexCollectiblesToAzureSearch((Collectibles)adItem, cancellationToken);
                            break;

                        case SubVertical.Deals:
                            await IndexDealsToAzureSearch((Deals)adItem, cancellationToken);
                            break;
                    }
                }

                return "The ad has been successfully marked as featured.";

            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error occurred while refreshing ad.");
                throw new ArgumentException(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred while refreshing ad.");
                throw new InvalidOperationException("Failed to refresh the ad due to an unexpected error.", ex);
            }
        }

        public async Task<string> PromoteClassifiedAd(ClassifiedsPromoteDto dto, string userId, Guid subscriptionid, CancellationToken cancellationToken)
        {
            _logger.LogInformation("PromoteClassifiedAd called. SubVertical: {SubVertical}, AdId: {AdId}, UserId: {UserId}", dto.SubVertical, dto.AdId, userId);

            try
            {
                subscriptionid = Guid.Parse("5a024f96-7414-4473-80b8-f5d70297e262");
                //var subcription = await _subscriptionContext.Subscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionid,cancellationToken);
                
                object? adItem = null;

                _logger.LogDebug("Fetching ad from database for SubVertical: {SubVertical}", dto.SubVertical);

                adItem = dto.SubVertical switch
                {
                    SubVertical.Items => await _context.Item
                        .FirstOrDefaultAsync(i => i.Id == dto.AdId && i.IsActive, cancellationToken),
                    SubVertical.Preloved => await _context.Preloved
                        .FirstOrDefaultAsync(p => p.Id == dto.AdId && p.IsActive, cancellationToken),
                    SubVertical.Collectibles => await _context.Collectible
                        .FirstOrDefaultAsync(c => c.Id == dto.AdId && c.IsActive, cancellationToken),
                    SubVertical.Deals => await _context.Deal
                        .FirstOrDefaultAsync(d => d.Id == dto.AdId && d.IsActive, cancellationToken),
                    _ => throw new InvalidOperationException($"Invalid SubVertical: {dto.SubVertical}")
                };

                _logger.LogDebug("Database fetch complete. Found ad: {Found}", adItem != null);

                if (adItem == null)
                {
                    _logger.LogError("Ad with id {AdId} not found in {SubVertical}.", dto.AdId, dto.SubVertical);
                    throw new KeyNotFoundException($"Ad with id {dto.AdId} not found.");
                }

                _logger.LogDebug("Updating promotion status for ad type: {Type}", adItem.GetType().Name);

                switch (adItem)
                {
                    case Items itemAd:
                        if (itemAd.IsPromoted)
                        {
                            _logger.LogWarning("Ad {AdId} in Items is already promoted.", itemAd.Id);
                            throw new ConflictException("This ad is already promoted.");
                        }
                        itemAd.IsPromoted = true;
                        itemAd.PromotedExpiryDate = DateTime.UtcNow.AddDays(30);
                        itemAd.UpdatedAt = DateTime.UtcNow;
                        itemAd.UpdatedBy = userId;
                        break;

                    case Preloveds prelovedAd:
                        if (prelovedAd.IsPromoted)
                        {
                            _logger.LogWarning("Ad {AdId} in Preloved is already promoted.", prelovedAd.Id);
                            throw new ConflictException("This ad is already promoted.");
                        }
                        prelovedAd.IsPromoted = true;
                        prelovedAd.PromotedExpiryDate = DateTime.UtcNow.AddDays(30);
                        prelovedAd.UpdatedAt = DateTime.UtcNow;
                        prelovedAd.UpdatedBy = userId;
                        break;

                    case Collectibles collectiblesAd:
                        if (collectiblesAd.IsPromoted)
                        {
                            _logger.LogWarning("Ad {AdId} in Collectibles is already promoted.", collectiblesAd.Id);
                            throw new ConflictException("This ad is already promoted.");
                        }
                        collectiblesAd.IsPromoted = true;
                        collectiblesAd.PromotedExpiryDate = DateTime.UtcNow.AddDays(30);
                        collectiblesAd.UpdatedAt = DateTime.UtcNow;
                        collectiblesAd.UpdatedBy = userId;
                        break;

                    case Deals dealsAd:
                        if (dealsAd.IsPromoted)
                        {
                            _logger.LogWarning("Ad {AdId} in Deals is already promoted.", dealsAd.Id);
                            throw new ConflictException("This ad is already promoted.");
                        }
                        dealsAd.IsPromoted = true;
                        dealsAd.PromotedExpiryDate = DateTime.UtcNow.AddDays(30);
                        dealsAd.UpdatedAt = DateTime.UtcNow;
                        dealsAd.UpdatedBy = userId;
                        break;

                    default:
                        _logger.LogError("Unsupported ad type: {Type}", adItem.GetType().Name);
                        throw new InvalidOperationException($"Unsupported ad type: {adItem.GetType().Name}");
                }

                _logger.LogDebug("Saving changes to database for AdId: {AdId}", dto.AdId);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Ad {AdId} successfully marked as promoted.", dto.AdId);
                
                _logger.LogDebug("Indexing promoted ad {AdId} in Azure Search for {SubVertical}", dto.AdId, dto.SubVertical);
                switch (dto.SubVertical)
                {
                    case SubVertical.Items:
                        await IndexItemsToAzureSearch((Items)adItem, cancellationToken);
                        break;
                    case SubVertical.Preloved:
                        await IndexPrelovedToAzureSearch((Preloveds)adItem, cancellationToken);
                        break;
                    case SubVertical.Collectibles:
                        await IndexCollectiblesToAzureSearch((Collectibles)adItem, cancellationToken);
                        break;
                    case SubVertical.Deals:
                        await IndexDealsToAzureSearch((Deals)adItem, cancellationToken);
                        break;
                }


                return "The ad has been successfully marked as promoted.";
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is KeyNotFoundException ||
                ex is InvalidOperationException ||
                ex is ConflictException)
            {
                _logger.LogError(ex, "Known error occurred while promoting ad with AdId: {AdId}", dto.AdId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred while promoting ad with AdId: {AdId}", dto.AdId);
                throw new InvalidOperationException("Failed to promote the ad due to an unexpected error.", ex);
            }
        }

        #region WishList

        public async Task<string> Favourite(WishlistCreateDto dto, string userId, CancellationToken cancellationToken)
        {
            try
            {
                var exists = await _context.Wishlists
                    .AnyAsync(w => w.UserId == userId && w.Vertical == dto.Vertical && w.AdId == dto.AdId);

                if (!exists)
                {
                    var wishlist = new Wishlist
                    {
                        UserId = userId,
                        Vertical = dto.Vertical,
                        Subvertical = dto.SubVertical,
                        AdId = dto.AdId
                    };
                    _context.Wishlists.Add(wishlist);
                    await _context.SaveChangesAsync();

                    return "Added to favourites successfully.";
                }
                else
                {
                    return "Already exists in favourites.";
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while Favourite.", ex);
            }
        }

        public async Task<List<Wishlist>> GetAllByUserFavouriteList(string userId, Vertical vertical, SubVertical subVertical, CancellationToken cancellationToken)
        {
            try
            {
                var list = await _context.Wishlists
                            .Where(w => w.UserId == userId && w.Vertical == vertical)
                            .Select(w => new Wishlist
                            {
                                Id = w.Id,
                                UserId = w.UserId,
                                Vertical = w.Vertical,
                                Subvertical = w.Subvertical,
                                AdId = w.AdId,
                                CreatedAt = w.CreatedAt,
                                UpdatedAt = w.UpdatedAt
                            }).ToListAsync();

                return list;
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException("An error occurred while retrieving the user's wishlist.", ex);
                throw;
            }
        }

        public async Task<string> UnFavourite(string userId, Vertical vertical, SubVertical subVertical, long adId, CancellationToken cancellationToken)
        {
            try
            {
                var wishList = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.Vertical == vertical && w.AdId == adId);

                if (wishList == null)
                    return "Wishlist item not found.";

                _context.Wishlists.Remove(wishList);
                await _context.SaveChangesAsync();

                return "Wishlist item removed successfully.";

            }            
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk publish/unpublish failed.");
                throw new InvalidOperationException("An error occurred while removing the wishlist item", ex);
            }
        }

        #endregion


    }
}
