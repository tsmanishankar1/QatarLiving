using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Service.FileStorage;
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


        private readonly ILogger<ClassifiedService> _logger;
        private readonly string itemJsonPath = Path.Combine("ClassifiedMockData", "itemsAdsMock.json");
        private readonly string prelovedJsonPath = Path.Combine("ClassifiedMockData", "prelovedAdsMock.json");
        private readonly string CollectablesonPath = Path.Combine("ClassifiedMockData", "collectables.json");
        public ClassifiedService(Dapr.Client.DaprClient dapr, ILogger<ClassifiedService> logger, IWebHostEnvironment env)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _env = env;
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

        public async Task<List<SavedSearchResponseDto>> GetSearches(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("UserId is required.");

                var key = $"search:{userId}";
                var result = await _dapr.GetStateAsync<List<SavedSearchResponseDto>>(UnifiedStore, key);

                return result ?? new List<SavedSearchResponseDto>();
            }
            catch (DaprException dex)
            {
                Console.WriteLine($"Dapr error: {dex.Message}");
                throw new InvalidOperationException("Failed to retrieve saved searches due to Dapr error.", dex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw new InvalidOperationException("An unexpected error occurred while retrieving saved searches.", ex);
            }
        }

        public Task<bool> SaveSearch(SaveSearchRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        private async Task<List<ItemAd>> ReadAllItemsAdsFromFile()
        {
            try
            {
                var jsonString = await File.ReadAllTextAsync(itemJsonPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<ItemAd>>(jsonString, options) ?? new();
            }
            catch
            {
                return new List<ItemAd>();
            }
        }

        public async Task<ItemAdsAndDashboardResponse> GetUserItemsAdsWithDashboard(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var allAds = await ReadAllItemsAdsFromFile();
                var userAds = allAds.Where(ad => ad.UserId == userId).ToList();
                               

                var groupedAds = new AdsGroupedResult
                {
                    PublishedAds = userAds
                        .Where(ad => ad.Status == AdStatus.Published)
                        .OrderByDescending(ad => ad.CreatedDate)
                        .ToList(),

                    UnpublishedAds = userAds
                        .Where(ad => ad.Status != AdStatus.Published)
                        .OrderByDescending(ad => ad.CreatedDate)
                        .ToList()
                };
                
                var publishedCount = userAds.Count(ad => ad.Status == AdStatus.Published);
                var promotedCount = userAds.Count(ad => ad.IsPromoted == true);
                var featuredCount = userAds.Count(ad => ad.IsFeatured == true);
                var refreshCount = userAds.Count(ad => ad.RefreshExpiry != null);
                var totalImpressions = userAds.Sum(ad => ad.Impressions ?? 0);
                var totalViews = userAds.Sum(ad => ad.Views ?? 0);
                var totalWhatsappClicks = userAds.Sum(ad => ad.WhatsAppClicks ?? 0);
                var totalCalls = userAds.Sum(ad => ad.Calls ?? 0);

                var adWithRefresh = userAds
                    .Where(ad => ad.RefreshExpiry != null)
                    .OrderByDescending(ad => ad.RefreshExpiry)
                    .FirstOrDefault();

                var dashboard = new ItemDashboardDto
                {
                    PublishedAds = publishedCount,
                    PromotedAds = promotedCount,
                    FeaturedAds = featuredCount,
                    Refreshes = refreshCount,
                    Impressions = totalImpressions,
                    Views = totalViews,
                    WhatsAppClicks = totalWhatsappClicks,
                    Calls = totalCalls,
                    RemainingRefreshes = adWithRefresh?.RemainingRefreshes ?? 0,
                    TotalAllowedRefreshes = adWithRefresh?.TotalAllowedRefreshes ?? 0,
                    RefreshExpiry = adWithRefresh?.RefreshExpiry
                };

                return new ItemAdsAndDashboardResponse
                {                    
                    ItemsDashboard = dashboard,
                    ItemsAds = groupedAds
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while retrieving item ads and dashboard summary.", ex);
            }
        }

        private async Task<List<PrelovedAd>> ReadAllPrelovedAdsFromFile()
        {
            try
            {                
                var jsonString = await File.ReadAllTextAsync(prelovedJsonPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<PrelovedAd>>(jsonString, options) ?? new();
            }
            catch
            {
                return new List<PrelovedAd>();
            }
        }

        public async Task<PrelovedAdsAndDashboardResponse> GetUserPrelovedAdsAndDashboard(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var allAds = await ReadAllPrelovedAdsFromFile();
                var userAds = allAds.Where(ad => ad.UserId == userId).ToList();

                var groupedAds = new AdsGroupedPrelovedResult
                {
                    PublishedAds = userAds
                        .Where(ad => ad.Status == AdStatus.Published)
                        .OrderByDescending(ad => ad.CreatedDate)
                        .ToList(),
                    UnpublishedAds = userAds
                        .Where(ad => ad.Status != AdStatus.Published)
                        .OrderByDescending(ad => ad.CreatedDate)
                        .ToList()
                };

                var dashboard = new PrelovedDashboardDto
                {
                    PublishedAds = userAds.Count(ad => ad.Status == AdStatus.Published),
                    PromotedAds = userAds.Count(ad => ad.IsPromoted == true),
                    FeaturedAds = userAds.Count(ad => ad.IsFeatured == true),
                    Impressions = userAds.Sum(ad => ad.Impressions ?? 0),
                    Views = userAds.Sum(ad => ad.Views ?? 0),
                    WhatsAppClicks = userAds.Sum(ad => ad.WhatsAppClicks ?? 0),
                    Calls = userAds.Sum(ad => ad.Calls ?? 0)
                };

                return new PrelovedAdsAndDashboardResponse
                {                                        
                    PrelovedAds = groupedAds,
                    PrelovedDashboard = dashboard
                };

            }
            catch(Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while generating Preloved ads and dashboard summary.", ex);
            }
        }

        public async Task<CollectiblesResponse> GetCollectibles(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID is required", nameof(userId));

                var fullPath = Path.Combine(_env.ContentRootPath, CollectablesonPath);

                if (!File.Exists(fullPath))
                    throw new FileNotFoundException("JSON data file not found", fullPath);

                var json = await File.ReadAllTextAsync(fullPath, cancellationToken);
                var allData = JsonSerializer.Deserialize<CollectiblesResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });


                if (allData == null)
                    return new CollectiblesResponse();

                var targetUserId = Guid.Parse(userId);

                if (allData.UserId != targetUserId)
                    return new CollectiblesResponse();

                return allData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading collectibles: {ex.Message}");
                throw;
            }
        }     
        
        public async Task<AdCreatedResponseDto> CreateClassifiedItemsAd(ClassifiedItems dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            if (dto.UserId != null) throw new ArgumentException("UserId is required.");

            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            if (dto.AdImagesBase64 == null || dto.AdImagesBase64.Count == 0)
                throw new ArgumentException("Image URLs must be provided.");

            if (string.IsNullOrWhiteSpace(dto.CertificateFileName))
                throw new ArgumentException("Certificate URL must be provided.");

            var adId = dto.Id != Guid.Empty ? dto.Id : throw new ArgumentException("Id must be provided");

            var key = $"ad-{adId}";

            try
            {                                            
                var existing = await _dapr.GetStateAsync<object>(UnifiedStore, key);
                if (existing != null)
                {
                    throw new InvalidOperationException($"Ad with key {key} already exists.");
                }

                var adItem = new
                {
                    Id = adId,
                    dto.SubVertical,
                    dto.Title,
                    dto.Description,
                    dto.CategoryId,
                    dto.Category,
                    dto.L1CategoryId,
                    L1Category = dto.l1Category,
                    dto.L2CategoryId,
                    L2Category = dto.L2Category,
                    dto.Brand,
                    dto.Model,
                    dto.Price,
                    dto.PriceType,
                    dto.Condition,
                    dto.Color,
                    dto.AcceptsOffers,
                    dto.MakeType,
                    dto.Capacity,
                    dto.Processor,
                    dto.Coverage,
                    dto.Ram,
                    dto.Resolution,
                    dto.BatteryPercentage,
                    dto.Size,
                    dto.SizeValue,
                    dto.Gender,
                    CertificateUrl = dto.CertificateFileName,
                    ImageUrls = dto.AdImagesBase64,
                    dto.PhoneNumber,
                    dto.WhatsAppNumber,
                    dto.Zone,
                    dto.StreetNumber,
                    dto.BuildingNumber,
                    dto.Latitude,
                    dto.Longitude,
                    dto.UserId,   
                    dto.IsFeatured,
                    dto.IsPromoted,
                    dto.TearmsAndCondition,
                    CreatedAt = DateTime.UtcNow,
                    ExpiryDate = dto.ExpiryDate,
                    RefreshExpiry = dto.RefreshExpiry,
                    Status = AdStatus.Draft
                };
                
                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, ItemsIndexKey) ?? new();
                index.Add(key);

                await _dapr.SaveStateAsync(UnifiedStore, key, adItem);
                await _dapr.SaveStateAsync(UnifiedStore, ItemsIndexKey, index);
                var classifiedsIndex = new ClassifiedsIndex
                {
                    Id = adId.ToString(),
                    SubVertical = dto.SubVertical,
                    Title = dto.Title,
                    Description = dto.Description,
                    CategoryId = dto.CategoryId.ToString(),
                    Category = dto.Category,
                    L1Category = dto.l1Category,
                    L2Category = dto.L2Category,
                    Price = (double?)dto.Price,
                    PriceType = dto.PriceType,
                    Location = dto.Location.FirstOrDefault(),
                    PhoneNumber = dto.PhoneNumber,
                    WhatsappNumber = dto.WhatsAppNumber,
                    UserId = dto.UserId.ToString(),
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Images = new List<ImageInfo>(),
                    Make = dto.MakeType,
                    Model = dto.Model,
                    Brand = dto.Brand,
                    Processor = dto.Processor,
                    Ram = dto.Ram,
                    SizeType = dto.Size,
                    Size = dto.SizeValue,
                    Status = "Active",
                    StreetNumber = dto.StreetNumber,
                    Zone = dto.Zone,
                    Storage = dto.Capacity,
                    BuildingNumber = dto.BuildingNumber,
                    Colour = dto.Color,
                    BatteryPercentage= dto.BatteryPercentage,
                    ExpiryDate = dto.ExpiryDate,
                    RefreshExpiryDate = dto.RefreshExpiry                                        
                };

               
                var msg = new IndexMessage
                {
                    Vertical = ConstantValues.Verticals.Classifieds,
                    Action = "Upsert", 
                    UpsertRequest = new CommonIndexRequest
                    {
                        VerticalName = ConstantValues.Verticals.Classifieds, 
                        ClassifiedsItem = classifiedsIndex
                    }
                };

                await _dapr.PublishEventAsync(ConstantValues.PubSubName, ConstantValues.PubSubTopics.IndexUpdates, msg, cancellationToken);
                return new AdCreatedResponseDto
                {
                    AdId = adId,
                    Title = dto.Title,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Items Ad created successfully"
                };
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                _logger.LogWarning(ex, "Duplicate ad insert attempt.");
                throw new InvalidOperationException("Ad already exists. Conflict occurred during Items ad creation.", ex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed in CreateClassifiedItemsAd");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Operation error while creating classified Items ad.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred during ad creation.");
                throw new InvalidOperationException("An unexpected error occurred while creating the Items ad. Please try again later.", ex);
            }
        }

        public async Task<AdCreatedResponseDto> CreateClassifiedPrelovedAd(ClassifiedPreloved dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            if (dto.UserId != null) throw new ArgumentException("UserId is required.");

            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            if (dto.AdImagesBase64 == null || dto.AdImagesBase64.Count == 0)
                throw new ArgumentException("Image URLs must be provided.");

            if (string.IsNullOrWhiteSpace(dto.CertificateFileName))
                throw new ArgumentException("Certificate URL must be provided.");

            var adId = dto.Id != Guid.Empty ? dto.Id : throw new ArgumentException("Id must be provided");

            var key = $"ad-{adId}";
            try
            {
                var existing = await _dapr.GetStateAsync<object>(UnifiedStore, key);
                if (existing != null)
                {
                    throw new InvalidOperationException($"Ad with key {key} already exists.");
                }
                var adItem = new
                {
                    Id = adId,
                    dto.SubVertical,
                    dto.Title,
                    dto.Description,
                    dto.CategoryId,
                    dto.Category,
                    dto.L1CategoryId,
                    L1Category = dto.l1Category,
                    dto.L2CategoryId,
                    L2Category = dto.L2Category,
                    dto.Brand,
                    dto.Model,
                    dto.Price,
                    dto.PriceType,
                    dto.Condition,
                    dto.Color,                    
                    dto.Capacity,
                    dto.Processor,
                    dto.Coverage,
                    dto.Ram,
                    dto.Resolution,
                    dto.BatteryPercentage,
                    dto.Size,
                    dto.SizeValue,
                    dto.Gender,
                    CertificateUrl = dto.CertificateFileName,
                    ImageUrls = dto.AdImagesBase64,
                    dto.PhoneNumber,
                    dto.WhatsAppNumber,
                    dto.Zone,
                    dto.StreetNumber,
                    dto.BuildingNumber,
                    dto.Latitude,
                    dto.Longitude,
                    dto.UserId,
                    dto.IsFeatured,
                    dto.IsPromoted,
                    CreatedAt = DateTime.UtcNow,
                    ExpiryDate = dto.ExpiryDate,
                    RefreshExpiry = dto.RefreshExpiry,
                    Status = AdStatus.Draft
                };

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, PrelovedIndexKey) ?? new();
                index.Add(key);

                await _dapr.SaveStateAsync(UnifiedStore, key, adItem);
                await _dapr.SaveStateAsync(UnifiedStore, PrelovedIndexKey, index);

                var prelovedIndex = new ClassifiedsIndex
                {
                    Id = adId.ToString(),
                    SubVertical = dto.SubVertical,
                    Title = dto.Title,
                    Description = dto.Description,
                    CategoryId = dto.CategoryId.ToString(),
                    Category = dto.Category,
                    L1Category = dto.l1Category,
                    L2Category = dto.L2Category,
                    Price = (double?)dto.Price,
                    PriceType = dto.PriceType,
                    Location = dto.Location.FirstOrDefault(),
                    PhoneNumber = dto.PhoneNumber,
                    WhatsappNumber = dto.WhatsAppNumber,
                    UserId = dto.UserId.ToString(),
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Images = new List<ImageInfo>(),
                    Status = "Active",
                    Model = dto.Model,
                    Brand = dto.Brand,
                    Processor = dto.Processor,
                    Ram = dto.Ram,
                    SizeType = dto.Size,
                    Size = dto.SizeValue,
                    StreetNumber = dto.StreetNumber,
                    Zone = dto.Zone,
                    Storage = dto.Capacity,
                    BuildingNumber = dto.BuildingNumber,
                    Colour = dto.Color,
                    BatteryPercentage = dto.BatteryPercentage,
                    ExpiryDate = dto.ExpiryDate,
                    RefreshExpiryDate = dto.RefreshExpiry
                };

                // Publish the indexing message using Pub/Sub
                var msg = new IndexMessage
                {
                    Vertical = ConstantValues.Verticals.Classifieds,
                    Action = "Upsert",
                    UpsertRequest = new CommonIndexRequest
                    {
                        VerticalName = ConstantValues.Verticals.Classifieds,
                        ClassifiedsItem = prelovedIndex
                    }
                };

                await _dapr.PublishEventAsync(ConstantValues.PubSubName, ConstantValues.PubSubTopics.IndexUpdates, msg, cancellationToken);

                return new AdCreatedResponseDto
                {
                    AdId = adId,
                    Title = dto.Title,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Preloved Ad created successfully"
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

        public async Task<AdCreatedResponseDto> CreateClassifiedCollectiblesAd(ClassifiedCollectibles dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            if (dto.UserId != null) throw new ArgumentException("UserId is required.");

            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            if (dto.AdImagesBase64 == null || dto.AdImagesBase64.Count == 0)
                throw new ArgumentException("Image URLs must be provided.");

            if (string.IsNullOrWhiteSpace(dto.CertificateFileName))
                throw new ArgumentException("Certificate URL must be provided.");

            var adId = dto.Id != Guid.Empty ? dto.Id : throw new ArgumentException("Id must be provided.");
            var key = $"ad-{adId}";

            try
            {
                var existing = await _dapr.GetStateAsync<object>(UnifiedStore, key);
                if (existing != null)
                    throw new InvalidOperationException($"Ad with key {key} already exists.");

                var adItem = new
                {
                    Id = adId,
                    dto.SubVertical,
                    dto.Title,
                    dto.Description,
                    dto.CategoryId,
                    dto.Category,
                    dto.L1CategoryId,
                    L1Category = dto.l1Category,
                    dto.L2CategoryId,
                    L2Category = dto.L2Category,
                    dto.Brand,
                    dto.Price,
                    dto.PriceType,
                    dto.Condition,
                    dto.CountryOfOrigin,
                    dto.Language,
                    dto.HasAuthenticityCertificate,
                    CertificateUrl = dto.CertificateFileName,
                    dto.YearOrEra,
                    dto.Rarity,
                    dto.Package,
                    dto.IsGraded,
                    dto.GradingCompany,
                    dto.Grades,
                    dto.Material,
                    dto.Scale,
                    dto.SerialNumber,
                    dto.Signed,
                    dto.SignedBy,
                    dto.FramedBy,
                    ImageUrls = dto.AdImagesBase64,
                    dto.PhoneNumber,
                    dto.WhatsAppNumber,
                    dto.ContactEmail,
                    dto.Location,
                    dto.StreetNumber,
                    dto.BuildingNumber,
                    dto.HasWarranty,
                    dto.IsHandmade,
                    dto.TearmsAndCondition,
                    dto.UserId,
                    dto.IsFeatured,
                    dto.IsPromoted,
                    ExpiryDate = dto.ExpiryDate,
                    RefreshExpiry = dto.RefreshExpiry,
                    CreatedAt = DateTime.UtcNow,
                    Status = AdStatus.Draft
                };

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, CollectiblesIndexKey) ?? new();
                index.Add(key);

                await _dapr.SaveStateAsync(UnifiedStore, key, adItem);
                await _dapr.SaveStateAsync(UnifiedStore, UnifiedIndexKey, index);
                var collectiblesIndex = new ClassifiedsIndex
                {
                    Id = adId.ToString(),
                    SubVertical = dto.SubVertical,
                    Title = dto.Title,
                    Description = dto.Description,
                    CategoryId = dto.CategoryId.ToString(),
                    Category = dto.Category,
                    L1Category = dto.l1Category,
                    L2Category = dto.L2Category,
                    Price = (double?)dto.Price,
                    PriceType = dto.PriceType,
                    Location = dto.Location.FirstOrDefault(),
                    PhoneNumber = dto.PhoneNumber,
                    WhatsappNumber = dto.WhatsAppNumber,
                    UserId = dto.UserId.ToString(),
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Images = new List<ImageInfo>(),
                    YearEra = dto.YearOrEra,
                    Rarity = dto.Rarity,
                    Material = dto.Material,
                    Status = "Active",
                    SerialNumber = dto.SerialNumber,
                    SignedBy = dto.SignedBy,
                    IsSigned = dto.Signed,
                    ExpiryDate = dto.ExpiryDate,
                    RefreshExpiryDate = dto.RefreshExpiry
                };

                // Publish the indexing message using Pub/Sub
                var msg = new IndexMessage
                {
                    Vertical = ConstantValues.Verticals.Classifieds,
                    Action = "Upsert",
                    UpsertRequest = new CommonIndexRequest
                    {
                        VerticalName = ConstantValues.Verticals.Classifieds,
                        ClassifiedsItem = collectiblesIndex
                    }
                };

                await _dapr.PublishEventAsync(ConstantValues.PubSubName, ConstantValues.PubSubTopics.IndexUpdates, msg, cancellationToken);

                return new AdCreatedResponseDto
                {
                    AdId = adId,
                    Title = dto.Title,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Collectibles Ad created successfully"
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

        public async Task<AdCreatedResponseDto> CreateClassifiedDealsAd(ClassifiedDeals dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            if (dto.UserId != null) throw new ArgumentException("UserId is required.");

            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            if (dto.AdImagesBase64 == null || dto.AdImagesBase64.Count == 0)
                throw new ArgumentException("Image URLs must be provided.");

            if (string.IsNullOrWhiteSpace(dto.FlyerName))
                throw new ArgumentException("Flyer URL must be provided.");

            var adId = dto.Id != Guid.Empty ? dto.Id : throw new ArgumentException("Id must be provided");

            var key = $"ad-{adId}";
            try
            {
                var existing = await _dapr.GetStateAsync<object>(UnifiedStore, key);
                if (existing != null)
                {
                    throw new InvalidOperationException($"Ad with key {key} already exists.");
                }
                var adItem = new
                {
                    Id = adId,
                    dto.SubVertical,
                    dto.Title,
                    FlyerFile = dto.FlyerName,
                    ImageUrl = dto.AdImagesBase64,
                    dto.XMLLink,
                    dto.ExpiryDate,
                    dto.PhoneNumber,
                    dto.WhatsAppNumber,
                    dto.Location,
                    dto.UserId,
                    dto.IsFeatured,
                    dto.IsPromoted,
                    CreatedAt = DateTime.UtcNow,
                    RefreshExpiry = dto.RefreshExpiry,
                    Status = AdStatus.Draft
                };

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, DealsIndexKey) ?? new();
                index.Add(key);

                await _dapr.SaveStateAsync(UnifiedStore, key, adItem);
                await _dapr.SaveStateAsync(UnifiedStore, DealsIndexKey, index);
                var dealsIndex = new ClassifiedsIndex
                {
                    Id = adId.ToString(),
                    SubVertical = dto.SubVertical,
                    Title = dto.Title,
                    Description = dto.Description,
                    Location = dto.Location.FirstOrDefault(),
                    CreatedDate = DateTime.UtcNow,
                    Images = new List<ImageInfo>(),
                    Status = "Active",
                    FlyerFileName = dto.FlyerName,
                    FlyerXmlLink = dto.XMLLink,
                    RefreshExpiryDate = dto.RefreshExpiry,
                    ExpiryDate = dto.ExpiryDate
                };

                // Publish the indexing message using Pub/Sub
                var msg = new IndexMessage
                {
                    Vertical = ConstantValues.Verticals.Classifieds,
                    Action = "Upsert",
                    UpsertRequest = new CommonIndexRequest
                    {
                        VerticalName = ConstantValues.Verticals.Classifieds,
                        ClassifiedsItem = dealsIndex
                    }
                };

                await _dapr.PublishEventAsync(ConstantValues.PubSubName, ConstantValues.PubSubTopics.IndexUpdates, msg, cancellationToken);

                return new AdCreatedResponseDto
                {
                    AdId = adId,
                    Title = dto.Title,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Deals Ad created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred during ad creation.");
                throw new InvalidOperationException("An unexpected error occurred while creating the Deals ad. Please try again later.", ex);
            }
        }

        public async Task<DeleteAdResponseDto> DeleteClassifiedItemsAd(Guid adId, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = $"ad-{adId}";

                var adObject = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);

                if (adObject.ValueKind != JsonValueKind.Object)
                    throw new KeyNotFoundException($"Ad with ID {adId} not found.");

                var subVertical = adObject.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                if (!string.Equals(subVertical, "Items", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Ad ID {adId} does not belong to the Items subvertical. Found: {subVertical}");

                _logger.LogInformation("Fetched ad object: {Json}", adObject.ToString());

                var blobNames = new List<string>();

                if (adObject.TryGetProperty("certificateUrl", out var certProp) && certProp.ValueKind == JsonValueKind.String)
                {
                    var certUrl = certProp.GetString();
                    var certBlobName = ExtractBlobName(certUrl);
                    if (!string.IsNullOrEmpty(certBlobName))
                        blobNames.Add(certBlobName);
                }

                if (adObject.TryGetProperty("imageUrls", out var imagesProp) && imagesProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var img in imagesProp.EnumerateArray())
                    {
                        if (img.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                        {
                            var imgUrl = urlProp.GetString();
                            var imgBlobName = ExtractBlobName(imgUrl);
                            if (!string.IsNullOrEmpty(imgBlobName))
                                blobNames.Add(imgBlobName);
                        }
                    }

                }

                _logger.LogInformation("Extracted blob names: {Blobs}", string.Join(", ", blobNames));

                await _dapr.DeleteStateAsync(UnifiedStore, key, cancellationToken: cancellationToken);

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, ItemsIndexKey, cancellationToken: cancellationToken) ?? new();
                if (index.Contains(key))
                {
                    index.Remove(key);
                    await _dapr.SaveStateAsync(UnifiedStore, ItemsIndexKey, index, cancellationToken: cancellationToken);
                }

                return new DeleteAdResponseDto
                {
                    Message = "Ad deleted successfully",
                    DeletedImages = blobNames
                };
            }
            catch (JsonException jex)
            {
                _logger.LogError(jex, "JSON parsing failed for ad ID: {AdId}", adId);
                throw new InvalidOperationException("Failed to parse ad JSON. Invalid format.", jex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting classified items ad with ID: {AdId}", adId);
                throw new InvalidOperationException("An unexpected error occurred while deleting the classified items ad.", ex);
            }
        }

        public async Task<DeleteAdResponseDto> DeleteClassifiedPrelovedAd(Guid adId, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = $"ad-{adId}";

                var adObject = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);

                if (adObject.ValueKind != JsonValueKind.Object)
                    throw new KeyNotFoundException($"Preloved Ad with ID {adId} not found.");

                var subVertical = adObject.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                if (!string.Equals(subVertical, "Preloved", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Ad ID {adId} does not belong to the Preloved subvertical. Found: {subVertical}");

                _logger.LogInformation("Fetched Preloved ad object: {Json}", adObject.ToString());

                var blobNames = new List<string>();

                if (adObject.TryGetProperty("certificateUrl", out var certProp) && certProp.ValueKind == JsonValueKind.String)
                {
                    var certUrl = certProp.GetString();
                    var certBlobName = ExtractBlobName(certUrl);
                    if (!string.IsNullOrEmpty(certBlobName))
                        blobNames.Add(certBlobName);
                }

                if (adObject.TryGetProperty("imageUrls", out var imagesProp) && imagesProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var img in imagesProp.EnumerateArray())
                    {
                        if (img.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                        {
                            var imgUrl = urlProp.GetString();
                            var imgBlobName = ExtractBlobName(imgUrl);
                            if (!string.IsNullOrEmpty(imgBlobName)) blobNames.Add(imgBlobName);
                        }
                    }
                }

                _logger.LogInformation("Extracted blob names for Preloved ad: {Blobs}", string.Join(", ", blobNames));

                await _dapr.DeleteStateAsync(UnifiedStore, key, cancellationToken: cancellationToken);

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, PrelovedIndexKey, cancellationToken: cancellationToken) ?? new();
                if (index.Contains(key))
                {
                    index.Remove(key);
                    await _dapr.SaveStateAsync(UnifiedStore, PrelovedIndexKey, index, cancellationToken: cancellationToken);
                }

                return new DeleteAdResponseDto
                {
                    Message = "Preloved Ad deleted successfully",
                    DeletedImages = blobNames
                };

            }
            catch (JsonException jex)
            {
                _logger.LogError(jex, "JSON parsing failed for Preloved ad ID: {AdId}", adId);
                throw new InvalidOperationException("Failed to parse Preloved ad JSON. Invalid format.", jex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting classified preloved ad with ID: {AdId}", adId);
                throw new InvalidOperationException("An unexpected error occurred while deleting the classified preloved ad.", ex);
            }
        }

        public async Task<DeleteAdResponseDto> DeleteClassifiedCollectiblesAd(Guid adId, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = $"ad-{adId}";

                var adObject = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);

                if (adObject.ValueKind != JsonValueKind.Object)
                    throw new KeyNotFoundException($"Collectibles Ad with ID {adId} not found.");

                var subVertical = adObject.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                if (!string.Equals(subVertical, "Collectibles", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Ad ID {adId} does not belong to the Collectibles subvertical. Found: {subVertical}");

                _logger.LogInformation("Fetched Collectibles ad object: {Json}", adObject.ToString());

                var blobNames = new List<string>();

                if (adObject.TryGetProperty("certificateUrl", out var certProp) && certProp.ValueKind == JsonValueKind.String)
                {
                    var certUrl = certProp.GetString();
                    var certBlobName = ExtractBlobName(certUrl);
                    if (!string.IsNullOrEmpty(certBlobName))
                        blobNames.Add(certBlobName);
                }

                if (adObject.TryGetProperty("imageUrls", out var imagesProp) && imagesProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var img in imagesProp.EnumerateArray())
                    {
                        if (img.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                        {
                            var imgUrl = urlProp.GetString();
                            var imgBlobName = ExtractBlobName(imgUrl);
                            if (!string.IsNullOrEmpty(imgBlobName)) blobNames.Add(imgBlobName);
                        }
                    }
                }

                _logger.LogInformation("Extracted blob names for Collectibles ad: {Blobs}", string.Join(", ", blobNames));

                await _dapr.DeleteStateAsync(UnifiedStore, key, cancellationToken: cancellationToken);

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, CollectiblesIndexKey, cancellationToken: cancellationToken) ?? new();
                if (index.Contains(key))
                {
                    index.Remove(key);
                    await _dapr.SaveStateAsync(UnifiedStore, CollectiblesIndexKey, index, cancellationToken: cancellationToken);
                }

                return new DeleteAdResponseDto
                {
                    Message = "Collectibles Ad deleted successfully",
                    DeletedImages = blobNames
                };
            }
            catch (JsonException jex)
            {
                _logger.LogError(jex, "JSON parsing failed for Collectibles ad ID: {AdId}", adId);
                throw new InvalidOperationException("Failed to parse Collectibles ad JSON. Invalid format.", jex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting classified collectibles ad with ID: {AdId}", adId);
                throw new InvalidOperationException("An unexpected error occurred while deleting the classified collectibles ad.", ex);
            }
        }

        public async Task<DeleteAdResponseDto> DeleteClassifiedDealsAd(Guid adId, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = $"ad-{adId}";

                var adObject = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);

                var subVertical = adObject.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                if (!string.Equals(subVertical, "Deals", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Ad ID {adId} does not belong to the Deals subvertical. Found: {subVertical}");

                if (adObject.ValueKind != JsonValueKind.Object)
                    throw new KeyNotFoundException($"Ad with ID {adId} not found.");

                _logger.LogInformation("Fetched deals ad object: {Json}", adObject.ToString());

                var blobNames = new List<string>();

                if (adObject.TryGetProperty("flyerFile", out var flyerProp) && flyerProp.ValueKind == JsonValueKind.String)
                {
                    var flyerUrl = flyerProp.GetString();
                    var flyerBlobName = ExtractBlobName(flyerUrl);
                    if (!string.IsNullOrEmpty(flyerBlobName))
                        blobNames.Add(flyerBlobName);
                }

                if (adObject.TryGetProperty("ImageUrl", out var imageUrlsProp) && imageUrlsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var img in imageUrlsProp.EnumerateArray())
                    {
                        if (img.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                        {
                            var imgUrl = urlProp.GetString();
                            var imgBlobName = ExtractBlobName(imgUrl);
                            if (!string.IsNullOrEmpty(imgBlobName)) blobNames.Add(imgBlobName);
                        }
                    }
                }
                _logger.LogInformation("Extracted blob names for deals ad: {Blobs}", string.Join(", ", blobNames));

                await _dapr.DeleteStateAsync(UnifiedStore, key, cancellationToken: cancellationToken);

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, DealsIndexKey, cancellationToken: cancellationToken) ?? new();

                if (index.Contains(key))
                {
                    index.Remove(key);
                    await _dapr.SaveStateAsync(UnifiedStore, DealsIndexKey, index, cancellationToken: cancellationToken);
                }

                return new DeleteAdResponseDto
                {
                    Message = "Deals Ad deleted successfully",
                    DeletedImages = blobNames
                };
            }
            catch (JsonException jex)
            {
                _logger.LogError(jex, "JSON parsing failed for Deals ad ID: {AdId}", adId);
                throw new InvalidOperationException("Failed to parse Deals ad JSON. Invalid format.", jex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting classified deals ad with ID: {AdId}", adId);
                throw new InvalidOperationException("An unexpected error occurred while deleting the classified deals ad.", ex);
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

        public async Task<PaginatedAdResponseDto> GetUserPublishedItemsAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            try
            {
                int currentPage = page ?? 1;
                int currentPageSize = pageSize ?? 10;


                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, ItemsIndexKey) ?? new();

                var publishedAds = new List<ItemAdDto>();                

                foreach (var key in index)
                {
                    try
                    {
                        var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);

                        if (state.ValueKind != JsonValueKind.Object)
                        {
                            _logger.LogWarning("Skipping key {Key} due to invalid state object.", key);
                            continue;
                        }

                        var subVertical = state.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                        var adUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : string.Empty;

                        if (!string.Equals(subVertical, "Items", StringComparison.OrdinalIgnoreCase) || adUserId != userId)
                            continue;

                        var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var statusInt)
                            ? (AdStatus)statusInt : AdStatus.Draft;

                        if (status != AdStatus.Published && status != AdStatus.Approved)
                            continue;

                        var title = state.GetProperty("title").GetString();
                        if (!string.IsNullOrWhiteSpace(search))
                        {
                            var normalizedTitle = title.Trim().ToLowerInvariant();
                            var normalizedSearch = search.Trim().ToLowerInvariant();
                            if (!normalizedTitle.Contains(normalizedSearch))
                            {
                                _logger.LogInformation("Ad with key {Key} skipped: title '{Title}' does not contain search term '{Search}'", key, title, search);
                                continue;
                            }
                            else
                            {
                                _logger.LogInformation("Ad with key {Key} included: title '{Title}' matches search '{Search}'", key, title, search);
                            }
                        }


                        var ad = new ItemAdDto
                        {
                            Id = state.GetProperty("id").GetGuid(),
                            Title = state.GetProperty("title").GetString(),
                            SubVertical = subVertical,
                            Description = state.GetProperty("description").GetString(),
                            Category = state.GetProperty("category").GetString(),
                            L1Category = state.GetProperty("l1Category").GetString(),
                            L2Category = state.TryGetProperty("l2Category", out var l2c) ? l2c.GetString() ?? "" : "",
                            Brand = state.GetProperty("brand").GetString(),
                            Model = state.GetProperty("model").GetString(),
                            Price = state.GetProperty("price").GetDecimal(),
                            PriceType = state.GetProperty("priceType").GetString(),
                            Condition = state.GetProperty("condition").GetString(),
                            Color = state.GetProperty("color").GetString(),
                            AcceptsOffers = state.TryGetProperty("acceptsOffers", out var offers) ? offers.GetString() ?? "" : "",
                            MakeType = state.TryGetProperty("makeType", out var make) ? make.GetString() ?? "" : "",
                            Capacity = state.TryGetProperty("capacity", out var capacity) ? capacity.GetString() ?? "" : "",
                            Processor = state.TryGetProperty("processor", out var processor) ? processor.GetString() ?? "" : "",
                            Coverage = state.TryGetProperty("coverage", out var coverage) ? coverage.GetString() ?? "" : "",
                            Ram = state.TryGetProperty("ram", out var ram) ? ram.GetString() ?? "" : "",
                            Resolution = state.TryGetProperty("resolution", out var res) ? res.GetString() ?? "" : "",
                            BatteryPercentage = state.TryGetProperty("batteryPercentage", out var battery) ? battery.GetRawText() : "",
                            Size = state.TryGetProperty("size", out var size) ? size.GetString() ?? "" : "",
                            SizeValue = state.TryGetProperty("sizeValue", out var sizeVal) ? sizeVal.GetString() ?? "" : "",
                            Gender = state.TryGetProperty("gender", out var gender) ? gender.GetString() ?? "" : "",
                            CertificateUrl = state.GetProperty("certificateUrl").GetString(),
                            ImageUrls = state.TryGetProperty("imageUrls", out var imgs) && imgs.ValueKind == JsonValueKind.Array
                            ? imgs.EnumerateArray().Select(img =>
                            {
                                var imageInfo = new ImageInfo
                                {
                                    AdImageFileNames = img.TryGetProperty("adImageFileNames", out var fn) ? fn.GetString() ?? "" : "",
                                    Url = img.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                                    Order = img.TryGetProperty("order", out var o) && o.TryGetInt32(out var ord) ? ord : 0
                                };
                                return imageInfo;
                            }).Where(i => !string.IsNullOrEmpty(i.Url)).ToList()
                            : new(),
                            Phone = state.TryGetProperty("phoneNumber", out var phone) ? phone.GetString() ?? "" : "",
                            WhatsAppNumber = state.TryGetProperty("whatsAppNumber", out var whatsapp) ? whatsapp.GetString() ?? "" : "",
                            Zone = state.TryGetProperty("zone", out var zone) ? zone.GetString() ?? "" : "",
                            StreetName = state.TryGetProperty("streetNumber", out var street) ? street.GetString() ?? "" : "",
                            BuildingNumber = state.TryGetProperty("buildingNumber", out var building) ? building.GetString() ?? "" : "",
                            Latitude = state.TryGetProperty("latitude", out var lat) ? lat.GetRawText() : "",
                            Longitude = state.TryGetProperty("longitude", out var lng) ? lng.GetRawText() : "",
                            CreatedAt = state.GetProperty("createdAt").GetDateTime(),
                            IsFeatured = state.TryGetProperty("isFeatured", out var featured) && featured.GetBoolean(),
                            IsPromoted = state.TryGetProperty("isPromoted", out var promoted) && promoted.GetBoolean(),
                            Status = status,
                            UserId = adUserId,
                            RefreshCount = state.TryGetProperty("refreshCount", out var refreshCount) ? refreshCount.GetString() : "80",
                            ExpiryDate = state.TryGetProperty("expiryDate", out var expiryDate) ? expiryDate.GetDateTime() : DateTime.MinValue,
                            RefreshExpiry = state.TryGetProperty("refreshExpiry", out var refreshExpiryDate) ? refreshExpiryDate.GetDateTime() : DateTime.MinValue
                        };

                        publishedAds.Add(ad);
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, "Error processing ad from key: {Key}", key);
                    }                   
                }

                publishedAds = sortOption switch
                {
                    AdSortOption.CreationDateOldest => publishedAds.OrderBy(a => a.CreatedAt).ToList(),
                    AdSortOption.CreationDateRecent => publishedAds.OrderByDescending(a => a.CreatedAt).ToList(),
                    AdSortOption.PriceHighToLow => publishedAds.OrderByDescending(a => a.Price).ToList(),
                    AdSortOption.PriceLowToHigh => publishedAds.OrderBy(a => a.Price).ToList(),
                    _ => publishedAds.OrderByDescending(a => a.CreatedAt).ToList(),
                };

                var total = publishedAds.Count;

                var pagedItems = publishedAds
                    .Skip((currentPage - 1) * currentPageSize)
                    .Take(currentPageSize)
                    .ToList();

                return new PaginatedAdResponseDto
                {
                    Total = total,
                    Items = pagedItems
                };
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve user items ads", ex);
            }
        }

        public async Task<PaginatedAdResponseDto> GetUserUnPublishedItemsAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            try
            {

                int currentPage = page ?? 1;
                int currentPageSize = pageSize ?? 10;

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, ItemsIndexKey) ?? new();

                var unpublishedAds = new List<ItemAdDto>();

                foreach (var key in index)
                {
                    try
                    {
                        var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);

                        if (state.ValueKind != JsonValueKind.Object) continue;

                        var subVertical = state.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                        var adUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : string.Empty;

                        if (string.IsNullOrWhiteSpace(subVertical) || !subVertical.Equals("Items", StringComparison.OrdinalIgnoreCase) || adUserId != userId)
                            continue;

                        _logger.LogDebug("Ad key {Key} - subVertical: {SubVertical}, userId: {UserId}", key, subVertical, adUserId);


                        var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var statusInt)
                            ? (AdStatus)statusInt : AdStatus.Draft;

                        if (status == AdStatus.Published || status == AdStatus.Approved)
                            continue;

                        var title = state.GetProperty("title").GetString();
                        if (!string.IsNullOrWhiteSpace(search) &&
                            (string.IsNullOrWhiteSpace(title) || !title.Contains(search, StringComparison.OrdinalIgnoreCase)))
                            continue;

                        var ad = new ItemAdDto
                        {
                            Id = state.GetProperty("id").GetGuid(),
                            Title = state.GetProperty("title").GetString(),
                            SubVertical = subVertical,
                            Description = state.GetProperty("description").GetString(),
                            Category = state.GetProperty("category").GetString(),
                            L1Category = state.GetProperty("l1Category").GetString(),
                            L2Category = state.TryGetProperty("l2Category", out var l2c) ? l2c.GetString() ?? "" : "",
                            Brand = state.GetProperty("brand").GetString(),
                            Model = state.GetProperty("model").GetString(),
                            Price = state.GetProperty("price").GetDecimal(),
                            PriceType = state.GetProperty("priceType").GetString(),
                            Condition = state.GetProperty("condition").GetString(),
                            Color = state.GetProperty("color").GetString(),
                            AcceptsOffers = state.TryGetProperty("acceptsOffers", out var offers) ? offers.GetString() ?? "" : "",
                            MakeType = state.TryGetProperty("makeType", out var make) ? make.GetString() ?? "" : "",
                            Capacity = state.TryGetProperty("capacity", out var capacity) ? capacity.GetString() ?? "" : "",
                            Processor = state.TryGetProperty("processor", out var processor) ? processor.GetString() ?? "" : "",
                            Coverage = state.TryGetProperty("coverage", out var coverage) ? coverage.GetString() ?? "" : "",
                            Ram = state.TryGetProperty("ram", out var ram) ? ram.GetString() ?? "" : "",
                            Resolution = state.TryGetProperty("resolution", out var res) ? res.GetString() ?? "" : "",
                            BatteryPercentage = state.TryGetProperty("batteryPercentage", out var battery) ? battery.GetRawText() : "",
                            Size = state.TryGetProperty("size", out var size) ? size.GetString() ?? "" : "",
                            SizeValue = state.TryGetProperty("sizeValue", out var sizeVal) ? sizeVal.GetString() ?? "" : "",
                            Gender = state.TryGetProperty("gender", out var gender) ? gender.GetString() ?? "" : "",
                            CertificateUrl = state.GetProperty("certificateUrl").GetString(),
                            ImageUrls = state.TryGetProperty("imageUrls", out var imgs) && imgs.ValueKind == JsonValueKind.Array
                            ? imgs.EnumerateArray().Select(img =>
                            {
                                var imageInfo = new ImageInfo
                                {
                                    AdImageFileNames = img.TryGetProperty("adImageFileNames", out var fn) ? fn.GetString() ?? "" : "",
                                    Url = img.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                                    Order = img.TryGetProperty("order", out var o) && o.TryGetInt32(out var ord) ? ord : 0
                                };
                                return imageInfo;
                            }).Where(i => !string.IsNullOrEmpty(i.Url)).ToList()
                            : new(),
                            Phone = state.TryGetProperty("phoneNumber", out var phone) ? phone.GetString() ?? "" : "",
                            WhatsAppNumber = state.TryGetProperty("whatsAppNumber", out var whatsapp) ? whatsapp.GetString() ?? "" : "",
                            Zone = state.TryGetProperty("zone", out var zone) ? zone.GetString() ?? "" : "",
                            StreetName = state.TryGetProperty("streetNumber", out var street) ? street.GetString() ?? "" : "",
                            BuildingNumber = state.TryGetProperty("buildingNumber", out var building) ? building.GetString() ?? "" : "",
                            Latitude = state.TryGetProperty("latitude", out var lat) ? lat.GetRawText() : "",
                            Longitude = state.TryGetProperty("longitude", out var lng) ? lng.GetRawText() : "",
                            CreatedAt = state.GetProperty("createdAt").GetDateTime(),
                            IsFeatured = state.TryGetProperty("isFeatured", out var featured) && featured.GetBoolean(),
                            IsPromoted = state.TryGetProperty("isPromoted", out var promoted) && promoted.GetBoolean(),
                            Status = status,
                            UserId = adUserId,
                            RefreshCount = state.TryGetProperty("refreshCount", out var refreshCount) ? refreshCount.GetString() : "80",
                            ExpiryDate = state.TryGetProperty("expiryDate", out var expiryDate) ? expiryDate.GetDateTime() : DateTime.MinValue,
                            RefreshExpiry = state.TryGetProperty("refreshExpiry", out var refreshExpiryDate) ? refreshExpiryDate.GetDateTime() : DateTime.MinValue
                        };

                        unpublishedAds.Add(ad);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing ad from key: {Key}", key);
                    }
                }

                unpublishedAds = sortOption switch
                {
                    AdSortOption.CreationDateOldest => unpublishedAds.OrderBy(a => a.CreatedAt).ToList(),
                    AdSortOption.CreationDateRecent => unpublishedAds.OrderByDescending(a => a.CreatedAt).ToList(),
                    AdSortOption.PriceHighToLow => unpublishedAds.OrderByDescending(a => a.Price).ToList(),
                    AdSortOption.PriceLowToHigh => unpublishedAds.OrderBy(a => a.Price).ToList(),
                    _ => unpublishedAds.OrderByDescending(a => a.CreatedAt).ToList()
                };

                var total = unpublishedAds.Count;

                var pagedItems = unpublishedAds
                    .Skip((currentPage - 1) * currentPageSize)
                    .Take(currentPageSize)
                    .ToList();

                return new PaginatedAdResponseDto
                {
                    Total = total,
                    Items = pagedItems
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve user items ads", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkUnpublishItemsAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, ItemsIndexKey) ?? new();
                var failedAds = new List<Guid>();

                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    if (!index.Contains(key))
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    if (state.ValueKind != JsonValueKind.Object)
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var storedUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : string.Empty;
                    var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var val) ? (AdStatus)val : AdStatus.Draft;

                    if (userId != userId || status != AdStatus.Published)
                    {
                        failedAds.Add(adId);
                    }
                }

                if (failedAds.Count > 0)
                {
                    return new BulkAdActionResponse
                    {
                        SuccessCount = 0,
                        FailedAdIds = failedAds,
                        Message = "Unpublish failed. Some ads are invalid"
                    };
                }

                int successCount = 0;
                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(state.ToString()!)!;
                    dict["status"] = (int)AdStatus.Unpublished;

                    await _dapr.SaveStateAsync(UnifiedStore, key, dict, cancellationToken: cancellationToken);
                    successCount++;
                }

                return new BulkAdActionResponse
                {
                    SuccessCount = successCount,
                    FailedAdIds = new(),
                    Message = $"{successCount} ad(s) unpublished successfully."
                };
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error occurred while bulk unpublishing ads.");
                throw new InvalidOperationException("An unexpected error occurred while bulk unpublishing ads.", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkPublishItemsAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, ItemsIndexKey) ?? new();
                var failedAds = new List<Guid>();
                
                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    if (!index.Contains(key))
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    if (state.ValueKind != JsonValueKind.Object)
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var storedUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : string.Empty;
                    var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var val) ? (AdStatus)val : AdStatus.Draft;

                    if (storedUserId != userId || (status != AdStatus.Unpublished && status != AdStatus.Draft))
                    {
                        failedAds.Add(adId);
                    }

                }

                if (failedAds.Count > 0)
                {
                    return new BulkAdActionResponse
                    {
                        SuccessCount = 0,
                        FailedAdIds = failedAds,
                        Message = "Publish failed. Some ads are invalid."
                    };
                }
                
                int successCount = 0;
                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(state.ToString()!)!;
                    dict["status"] = (int)AdStatus.Published;

                    await _dapr.SaveStateAsync(UnifiedStore, key, dict, cancellationToken: cancellationToken);
                    successCount++;
                }

                return new BulkAdActionResponse
                {
                    SuccessCount = successCount,
                    FailedAdIds = new(),
                    Message = $"{successCount} ad(s) published successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while bulk publishing ads.");
                throw new InvalidOperationException("An unexpected error occurred while bulk publishing ads.", ex);
            }
        }

        public async Task<PaginatedPrelovedAdResponseDto> GetUserPublishedPrelovedAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            try
            {
                int currentPage = page ?? 1;
                int currentPageSize = pageSize ?? 10;

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, PrelovedIndexKey) ?? new();
                var published = new List<PrelovedAdDto>();

                foreach (var key in index)
                {
                    try
                    {
                        var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);
                        if (state.ValueKind != JsonValueKind.Object) continue;

                        var subVertical = state.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                        var adUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : string.Empty;

                        if (!string.Equals(subVertical, "Preloved", StringComparison.OrdinalIgnoreCase) || adUserId != userId)
                            continue;

                        var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var statusInt)
                            ? (AdStatus)statusInt : AdStatus.Draft;

                        if (status != AdStatus.Published && status != AdStatus.Approved)
                            continue;

                        var title = state.GetProperty("title").GetString();
                        if (!string.IsNullOrWhiteSpace(search) &&
                            (string.IsNullOrWhiteSpace(title) || !title.Contains(search, StringComparison.OrdinalIgnoreCase)))
                            continue;

                        var ad = new PrelovedAdDto
                        {
                            Id = state.GetProperty("id").GetGuid(),
                            Title = state.GetProperty("title").GetString(),
                            SubVertical = subVertical,
                            Description = state.GetProperty("description").GetString(),
                            Category = state.GetProperty("category").GetString(),
                            L1Category = state.GetProperty("l1Category").GetString(),
                            L2Category = state.TryGetProperty("l2Category", out var l2) ? l2.GetString() ?? "" : "",
                            Brand = state.GetProperty("brand").GetString(),
                            Model = state.GetProperty("model").GetString(),
                            Price = state.GetProperty("price").GetDecimal(),
                            PriceType = state.GetProperty("priceType").GetString(),
                            Condition = state.GetProperty("condition").GetString(),
                            Color = state.GetProperty("color").GetString(),
                            Capacity = state.TryGetProperty("capacity", out var capacity) ? capacity.GetString() ?? "" : "",
                            Processor = state.TryGetProperty("processor", out var processor) ? processor.GetString() ?? "" : "",
                            Coverage = state.TryGetProperty("coverage", out var coverage) ? coverage.GetString() ?? "" : "",
                            Ram = state.TryGetProperty("ram", out var ram) ? ram.GetString() ?? "" : "",
                            Resolution = state.TryGetProperty("resolution", out var resolution) ? resolution.GetString() ?? "" : "",
                            BatteryPercentage = state.TryGetProperty("batteryPercentage", out var battery) ? battery.GetRawText() : "",
                            Size = state.TryGetProperty("size", out var size) ? size.GetString() ?? "" : "",
                            SizeValue = state.TryGetProperty("sizeValue", out var sizeVal) ? sizeVal.GetString() ?? "" : "",
                            Gender = state.TryGetProperty("gender", out var gender) ? gender.GetString() ?? "" : "",
                            CertificateUrl = state.GetProperty("certificateUrl").GetString(),
                            ImageUrls = state.TryGetProperty("imageUrls", out var imgs) && imgs.ValueKind == JsonValueKind.Array
                            ? imgs.EnumerateArray().Select(img =>
                            {
                                var imageInfo = new ImageInfo
                                {
                                    AdImageFileNames = img.TryGetProperty("adImageFileNames", out var fn) ? fn.GetString() ?? "" : "",
                                    Url = img.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                                    Order = img.TryGetProperty("order", out var o) && o.TryGetInt32(out var ord) ? ord : 0
                                };
                                return imageInfo;
                            }).Where(i => !string.IsNullOrWhiteSpace(i.Url)).ToList(): new(),
                            PhoneNumber = state.TryGetProperty("phoneNumber", out var phone) ? phone.GetString() ?? "" : "",
                            WhatsAppNumber = state.TryGetProperty("whatsAppNumber", out var whatsapp) ? whatsapp.GetString() ?? "" : "",
                            Zone = state.TryGetProperty("zone", out var zone) ? zone.GetString() ?? "" : "",
                            StreetNumber = state.TryGetProperty("streetNumber", out var street) ? street.GetString() ?? "" : "",
                            BuildingNumber = state.TryGetProperty("buildingNumber", out var building) ? building.GetString() ?? "" : "",
                            Latitude = state.TryGetProperty("latitude", out var lat) ? lat.GetRawText() : "",
                            Longitude = state.TryGetProperty("longitude", out var lng) ? lng.GetRawText() : "",
                            CreatedAt = state.GetProperty("createdAt").GetDateTime(),
                            IsFeatured = state.TryGetProperty("isFeatured", out var featured) && featured.GetBoolean(),
                            IsPromoted = state.TryGetProperty("isPromoted", out var promoted) && promoted.GetBoolean(),
                            Status = status,
                            UserId = adUserId,
                            RefreshCount = state.TryGetProperty("refreshCount", out var refreshCount) ? refreshCount.GetString() : "80",
                            ExpiryDate = state.TryGetProperty("expiryDate", out var expiryDate) ? expiryDate.GetDateTime() : DateTime.MinValue,
                            RefreshExpiry = state.TryGetProperty("refreshExpiry", out var refreshExpiryDate) ? refreshExpiryDate.GetDateTime() : DateTime.MinValue
                        };

                        published.Add(ad);

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing ad from key: {Key}", key); 
                    }
                    
                }

                published = sortOption switch
                {
                    AdSortOption.CreationDateOldest => published.OrderBy(a => a.CreatedAt).ToList(),
                    AdSortOption.CreationDateRecent => published.OrderByDescending(a => a.CreatedAt).ToList(),
                    AdSortOption.PriceHighToLow => published.OrderByDescending(a => a.Price).ToList(),
                    AdSortOption.PriceLowToHigh => published.OrderBy(a => a.Price).ToList(),
                    _ => published.OrderByDescending(a => a.CreatedAt).ToList(),
                };

                var total = published.Count;
                var pagedItems = published
                                 .Skip((currentPage - 1) * currentPageSize)
                                 .Take(currentPageSize)
                                 .ToList();

                return new PaginatedPrelovedAdResponseDto
                {
                    Total = total,
                    Items = pagedItems
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve user Preloved ads", ex);
            }
        }

        public async Task<PaginatedPrelovedAdResponseDto> GetUserUnPublishedPrelovedAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            try
            {
                int currentPage = page ?? 1;
                int currentPageSize = pageSize ?? 10;

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, PrelovedIndexKey) ?? new();
                var unpublished = new List<PrelovedAdDto>();

                foreach (var key in index)
                {
                    try
                    {
                        var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);
                        if (state.ValueKind != JsonValueKind.Object) continue;

                        var subVertical = state.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                        var adUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : String.Empty;

                        if (string.IsNullOrWhiteSpace(subVertical) ||
                            !subVertical.Equals("Preloved", StringComparison.OrdinalIgnoreCase) ||
                            adUserId != userId)
                            continue;

                        var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var statusInt)
                            ? (AdStatus)statusInt : AdStatus.Draft;

                        if (status == AdStatus.Published || status == AdStatus.Approved)
                            continue;

                        var title = state.GetProperty("title").GetString();
                        if (!string.IsNullOrWhiteSpace(search) && !title.Contains(search, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var ad = new PrelovedAdDto
                        {
                            Id = state.GetProperty("id").GetGuid(),
                            Title = state.GetProperty("title").GetString(),
                            SubVertical = subVertical,
                            Description = state.GetProperty("description").GetString(),
                            Category = state.GetProperty("category").GetString(),
                            L1Category = state.GetProperty("l1Category").GetString(),
                            L2Category = state.TryGetProperty("l2Category", out var l2) ? l2.GetString() ?? "" : "",
                            Brand = state.GetProperty("brand").GetString(),
                            Model = state.GetProperty("model").GetString(),
                            Price = state.GetProperty("price").GetDecimal(),
                            PriceType = state.GetProperty("priceType").GetString(),
                            Condition = state.GetProperty("condition").GetString(),
                            Color = state.GetProperty("color").GetString(),
                            Capacity = state.TryGetProperty("capacity", out var capacity) ? capacity.GetString() ?? "" : "",
                            Processor = state.TryGetProperty("processor", out var processor) ? processor.GetString() ?? "" : "",
                            Coverage = state.TryGetProperty("coverage", out var coverage) ? coverage.GetString() ?? "" : "",
                            Ram = state.TryGetProperty("ram", out var ram) ? ram.GetString() ?? "" : "",
                            Resolution = state.TryGetProperty("resolution", out var resolution) ? resolution.GetString() ?? "" : "",
                            BatteryPercentage = state.TryGetProperty("batteryPercentage", out var battery) ? battery.GetRawText() : "",
                            Size = state.TryGetProperty("size", out var size) ? size.GetString() ?? "" : "",
                            SizeValue = state.TryGetProperty("sizeValue", out var sizeVal) ? sizeVal.GetString() ?? "" : "",
                            Gender = state.TryGetProperty("gender", out var gender) ? gender.GetString() ?? "" : "",
                            CertificateUrl = state.GetProperty("certificateUrl").GetString(),
                            ImageUrls = state.TryGetProperty("imageUrls", out var imgs) && imgs.ValueKind == JsonValueKind.Array
                            ? imgs.EnumerateArray().Select(img =>
                            {
                                var imageInfo = new ImageInfo
                                {
                                    AdImageFileNames = img.TryGetProperty("adImageFileNames", out var fn) ? fn.GetString() ?? "" : "",
                                    Url = img.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                                    Order = img.TryGetProperty("order", out var o) && o.TryGetInt32(out var ord) ? ord : 0
                                };
                                return imageInfo;
                            }).Where(i => !string.IsNullOrWhiteSpace(i.Url)).ToList() : new(),
                            PhoneNumber = state.TryGetProperty("phoneNumber", out var phone) ? phone.GetString() ?? "" : "",
                            WhatsAppNumber = state.TryGetProperty("whatsAppNumber", out var whatsapp) ? whatsapp.GetString() ?? "" : "",
                            Zone = state.TryGetProperty("zone", out var zone) ? zone.GetString() ?? "" : "",
                            StreetNumber = state.TryGetProperty("streetNumber", out var street) ? street.GetString() ?? "" : "",
                            BuildingNumber = state.TryGetProperty("buildingNumber", out var building) ? building.GetString() ?? "" : "",
                            Latitude = state.TryGetProperty("latitude", out var lat) ? lat.GetRawText() : "",
                            Longitude = state.TryGetProperty("longitude", out var lng) ? lng.GetRawText() : "",
                            CreatedAt = state.GetProperty("createdAt").GetDateTime(),
                            IsFeatured = state.TryGetProperty("isFeatured", out var featured) && featured.GetBoolean(),
                            IsPromoted = state.TryGetProperty("isPromoted", out var promoted) && promoted.GetBoolean(),
                            Status = status,
                            UserId = adUserId,
                            RefreshCount = state.TryGetProperty("refreshCount", out var refreshCount) ? refreshCount.GetString() : "80",
                            ExpiryDate = state.TryGetProperty("expiryDate", out var expiryDate) ? expiryDate.GetDateTime() : DateTime.MinValue,
                            RefreshExpiry = state.TryGetProperty("refreshExpiry", out var refreshExpiryDate) ? refreshExpiryDate.GetDateTime() : DateTime.MinValue
                        };

                        unpublished.Add(ad);

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing ad from key: {Key}", key);
                    }

                }

                unpublished = sortOption switch
                {
                    AdSortOption.CreationDateOldest => unpublished.OrderBy(a => a.CreatedAt).ToList(),
                    AdSortOption.CreationDateRecent => unpublished.OrderByDescending(a => a.CreatedAt).ToList(),
                    AdSortOption.PriceHighToLow => unpublished.OrderByDescending(a => a.Price).ToList(),
                    AdSortOption.PriceLowToHigh => unpublished.OrderBy(a => a.Price).ToList(),
                    _ => unpublished.OrderByDescending(a => a.CreatedAt).ToList()
                };

                var total = unpublished.Count;
                var pagedItems = unpublished
                                 .Skip((currentPage - 1) * currentPageSize)
                                 .Take(currentPageSize)
                                 .ToList();

                return new PaginatedPrelovedAdResponseDto
                {
                    Total = total,
                    Items = pagedItems
                };
               
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve user Preloved ads", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkPublishPrelovedAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, PrelovedIndexKey) ?? new();
                var failedAds = new List<Guid>();

                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    if (!index.Contains(key))
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    if (state.ValueKind != JsonValueKind.Object)
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var storedUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : String.Empty;
                    var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var val) ? (AdStatus)val : AdStatus.Draft;

                    if (storedUserId != userId || (status != AdStatus.Unpublished && status != AdStatus.Draft))
                    {
                        failedAds.Add(adId);
                    }
                }

                if (failedAds.Count > 0)
                {
                    return new BulkAdActionResponse
                    {
                        SuccessCount = 0,
                        FailedAdIds = failedAds,
                        Message = "Publish failed. Some ads are invalid."
                    };
                }

                int successCount = 0;
                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(state.ToString()!)!;
                    dict["status"] = (int)AdStatus.Published;

                    await _dapr.SaveStateAsync(UnifiedStore, key, dict, cancellationToken: cancellationToken);
                    successCount++;
                }

                return new BulkAdActionResponse
                {
                    SuccessCount = successCount,
                    FailedAdIds = new(),
                    Message = $"{successCount} ad(s) published successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while bulk publishing preloved ads.");
                throw new InvalidOperationException("An unexpected error occurred while bulk publishing preloved ads.", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkUnpublishPrelovedAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, PrelovedIndexKey) ?? new();
                var failedAds = new List<Guid>();

                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    if (!index.Contains(key))
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    if (state.ValueKind != JsonValueKind.Object)
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var storedUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : String.Empty;
                    var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var val) ? (AdStatus)val : AdStatus.Draft;

                    if (storedUserId != userId || status != AdStatus.Published)
                    {
                        failedAds.Add(adId);
                    }
                }

                if (failedAds.Count > 0)
                {
                    return new BulkAdActionResponse
                    {
                        SuccessCount = 0,
                        FailedAdIds = failedAds,
                        Message = "Unpublish failed. Some ads are invalid."
                    };
                }

                int successCount = 0;
                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(state.ToString()!)!;
                    dict["status"] = (int)AdStatus.Unpublished;

                    await _dapr.SaveStateAsync(UnifiedStore, key, dict, cancellationToken: cancellationToken);
                    successCount++;
                }

                return new BulkAdActionResponse
                {
                    SuccessCount = successCount,
                    FailedAdIds = new(),
                    Message = $"{successCount} ad(s) unpublished successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while bulk unpublishing preloved ads.");
                throw new InvalidOperationException("An unexpected error occurred while bulk unpublishing preloved ads.", ex);
            }
        }

        public async Task<PaginatedDealsAdResponseDto> GetUserPublishedDealsAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            try
            {
                int currentPage = page ?? 1;
                int currentPageSize = pageSize ?? 10;

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, DealsIndexKey) ?? new();
                var published = new List<DealsAdDto>();


                foreach (var key in index)
                {
                    try
                    {
                        var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                        if (state.ValueKind != JsonValueKind.Object) continue;

                        var subVertical = state.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                        var adUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : String.Empty;

                        if (!string.Equals(subVertical, "Deals", StringComparison.OrdinalIgnoreCase) || adUserId != userId)
                            continue;

                        var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var statusInt)
                            ? (AdStatus)statusInt : AdStatus.Draft;

                        if (status != AdStatus.Published && status != AdStatus.Approved)
                            continue;

                        var title = state.GetProperty("title").GetString();
                        if (!string.IsNullOrWhiteSpace(search) && !title.Contains(search, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var ad = new DealsAdDto
                        {
                            Id = state.GetProperty("id").GetGuid(),
                            Title = state.GetProperty("title").GetString(),
                            SubVertical = subVertical ?? "Deals",
                            FlyerFile = state.TryGetProperty("flyerFile", out var flyer) ? flyer.GetString() ?? "" : "",
                            ImageUrl = state.TryGetProperty("imageUrl", out var imgs) && imgs.ValueKind == JsonValueKind.Array
                            ? imgs.EnumerateArray().Select(img =>
                            {
                                return new ImageInfo
                                {
                                    AdImageFileNames = img.TryGetProperty("adImageFileNames", out var fn) ? fn.GetString() ?? "" : "",
                                    Url = img.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                                    Order = img.TryGetProperty("order", out var o) && o.TryGetInt32(out var ord) ? ord : 0
                                };
                            }).Where(i => !string.IsNullOrWhiteSpace(i.Url)).ToList(): new(),
                            XMLLink = state.TryGetProperty("xmlLink", out var xml) ? xml.GetString() ?? "" : "",
                            ExpiryDate = state.TryGetProperty("expiryDate", out var expiry) && expiry.ValueKind == JsonValueKind.String
                                ? expiry.GetDateTime()
                                : DateTime.MinValue,
                            PhoneNumber = state.TryGetProperty("phoneNumber", out var phone) ? phone.GetString() ?? "" : "",
                            WhatsAppNumber = state.TryGetProperty("whatsAppNumber", out var whatsapp) ? whatsapp.GetString() ?? "" : "",
                            Location = state.TryGetProperty("location", out var loc) && loc.ValueKind == JsonValueKind.Array
                                ? loc.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrEmpty(x)).ToList()
                                : new(),
                            UserId = adUserId,
                            IsFeatured = state.TryGetProperty("isFeatured", out var featured) && featured.GetBoolean(),
                            IsPromoted = state.TryGetProperty("isPromoted", out var promoted) && promoted.GetBoolean(),
                            CreatedAt = state.TryGetProperty("createdAt", out var created) && created.ValueKind == JsonValueKind.String
                                ? created.GetDateTime()
                                : DateTime.UtcNow,
                            Status = status,
                            RefreshCount = state.TryGetProperty("refreshCount", out var refreshCount) ? refreshCount.GetString() : "80",                            
                            RefreshExpiry = state.TryGetProperty("refreshExpiry", out var refreshExpiryDate) ? refreshExpiryDate.GetDateTime() : DateTime.MinValue
                        };

                        published.Add(ad);
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, "Error processing ad from key: {Key}", key);
                    }
                }

                published = sortOption switch
                {
                    AdSortOption.CreationDateOldest => published.OrderBy(a => a.CreatedAt).ToList(),
                    AdSortOption.CreationDateRecent => published.OrderByDescending(a => a.CreatedAt).ToList(),                    
                    _ => published.OrderByDescending(a => a.CreatedAt).ToList(),
                };

                var total = published.Count;
                var pagedItems = published
                                 .Skip((currentPage - 1) * currentPageSize)
                                 .Take(currentPageSize)
                                 .ToList();

                return new PaginatedDealsAdResponseDto
                {
                    Total = total,
                    Items = pagedItems
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve user Deals ads", ex);
            }
        } 

        public async Task<PaginatedDealsAdResponseDto> GetUserUnPublishedDealsAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            try
            {
                int currentPage = page ?? 1;
                int currentPageSize = pageSize ?? 10;

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, DealsIndexKey) ?? new();
                var unpublished = new List<DealsAdDto>();


                foreach (var key in index)
                {
                    try
                    {
                        var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                        if (state.ValueKind != JsonValueKind.Object) continue;

                        var subVertical = state.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                        var adUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : String.Empty;

                        if (!string.Equals(subVertical, "Deals", StringComparison.OrdinalIgnoreCase) || adUserId != userId)
                            continue;

                        var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var statusInt)
                            ? (AdStatus)statusInt : AdStatus.Draft;

                        if (status == AdStatus.Published || status == AdStatus.Approved)
                            continue;

                        var title = state.GetProperty("title").GetString();
                        if (!string.IsNullOrWhiteSpace(search) && !title.Contains(search, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var ad = new DealsAdDto
                        {
                            Id = state.GetProperty("id").GetGuid(),
                            Title = state.GetProperty("title").GetString(),
                            SubVertical = subVertical ?? "Deals",
                            FlyerFile = state.TryGetProperty("flyerFile", out var flyer) ? flyer.GetString() ?? "" : "",
                            ImageUrl = state.TryGetProperty("imageUrl", out var imgs) && imgs.ValueKind == JsonValueKind.Array
                            ? imgs.EnumerateArray().Select(img =>
                            {
                                return new ImageInfo
                                {
                                    AdImageFileNames = img.TryGetProperty("adImageFileNames", out var fn) ? fn.GetString() ?? "" : "",
                                    Url = img.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                                    Order = img.TryGetProperty("order", out var o) && o.TryGetInt32(out var ord) ? ord : 0
                                };
                            }).Where(i => !string.IsNullOrWhiteSpace(i.Url)).ToList(): new(),
                            XMLLink = state.TryGetProperty("xmlLink", out var xml) ? xml.GetString() ?? "" : "",
                            ExpiryDate = state.TryGetProperty("expiryDate", out var expiry) && expiry.ValueKind == JsonValueKind.String
                                ? expiry.GetDateTime()
                                : DateTime.MinValue,
                            PhoneNumber = state.TryGetProperty("phoneNumber", out var phone) ? phone.GetString() ?? "" : "",
                            WhatsAppNumber = state.TryGetProperty("whatsAppNumber", out var whatsapp) ? whatsapp.GetString() ?? "" : "",
                            Location = state.TryGetProperty("location", out var loc) && loc.ValueKind == JsonValueKind.Array
                                ? loc.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrEmpty(x)).ToList()
                                : new(),
                            UserId = adUserId,
                            IsFeatured = state.TryGetProperty("isFeatured", out var featured) && featured.GetBoolean(),
                            IsPromoted = state.TryGetProperty("isPromoted", out var promoted) && promoted.GetBoolean(),
                            CreatedAt = state.TryGetProperty("createdAt", out var created) && created.ValueKind == JsonValueKind.String
                                ? created.GetDateTime()
                                : DateTime.UtcNow,
                            Status = status,
                            RefreshCount = state.TryGetProperty("refreshCount", out var refreshCount) ? refreshCount.GetString() : "80",                           
                            RefreshExpiry = state.TryGetProperty("refreshExpiry", out var refreshExpiryDate) ? refreshExpiryDate.GetDateTime() : DateTime.MinValue
                        };


                        unpublished.Add(ad);
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, "Error processing ad from key: {Key}", key);
                    }
                }

                unpublished = sortOption switch
                {
                    AdSortOption.CreationDateOldest => unpublished.OrderBy(a => a.CreatedAt).ToList(),
                    AdSortOption.CreationDateRecent => unpublished.OrderByDescending(a => a.CreatedAt).ToList(),                    
                    _ => unpublished.OrderByDescending(a => a.CreatedAt).ToList()
                };


                var total = unpublished.Count;
                var pagedItems = unpublished
                                 .Skip((currentPage - 1) * currentPageSize)
                                 .Take(currentPageSize)
                                 .ToList();

                return new PaginatedDealsAdResponseDto
                {
                    Total = total,
                    Items = pagedItems
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve user Deals ads", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkPublishDealsAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, DealsIndexKey) ?? new();
                var failedAds = new List<Guid>();

                foreach (var adId in adIds)
                {                    
                    var key = $"ad-{adId}";
                    if (!index.Contains(key))
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    if (state.ValueKind != JsonValueKind.Object)
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var storedUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : String.Empty;
                    var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var val) ? (AdStatus)val : AdStatus.Draft;

                    if (storedUserId != userId || (status != AdStatus.Unpublished && status != AdStatus.Draft))
                    {
                        failedAds.Add(adId);
                    }
                }

                if (failedAds.Count > 0)
                {
                    return new BulkAdActionResponse
                    {
                        SuccessCount = 0,
                        FailedAdIds = failedAds,
                        Message = "Publish failed. Some ads are invalid."
                    };
                }

                int successCount = 0;
                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(state.ToString()!)!;
                    dict["status"] = (int)AdStatus.Published;

                    await _dapr.SaveStateAsync(UnifiedStore, key, dict, cancellationToken: cancellationToken);
                    successCount++;
                }

                return new BulkAdActionResponse
                {
                    SuccessCount = successCount,
                    FailedAdIds = new(),
                    Message = $"{successCount} ad(s) published successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while bulk publishing deals ads.");
                throw new InvalidOperationException("An unexpected error occurred while bulk publishing deals ads.", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkUnpublishDealsAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, DealsIndexKey) ?? new();
                var failedAds = new List<Guid>();

                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    if (!index.Contains(key))
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    if (state.ValueKind != JsonValueKind.Object)
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var storedUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : String.Empty;
                    var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var val) ? (AdStatus)val : AdStatus.Draft;

                    if (storedUserId != userId || status != AdStatus.Published)
                    {
                        failedAds.Add(adId);
                    }
                }

                if (failedAds.Count > 0)
                {
                    return new BulkAdActionResponse
                    {
                        SuccessCount = 0,
                        FailedAdIds = failedAds,
                        Message = "Unpublish failed. Some ads are invalid."
                    };
                }

                int successCount = 0;
                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(state.ToString()!)!;
                    dict["status"] = (int)AdStatus.Unpublished;

                    await _dapr.SaveStateAsync(UnifiedStore, key, dict, cancellationToken: cancellationToken);
                    successCount++;
                }

                return new BulkAdActionResponse
                {
                    SuccessCount = successCount,
                    FailedAdIds = new(),
                    Message = $"{successCount} ad(s) unpublished successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while bulk unpublishing deals ads.");
                throw new InvalidOperationException("An unexpected error occurred while bulk unpublishing deals ads.", ex);
            }
        }

        public async Task<PaginatedCollectiblesAdResponseDto> GetUserPublishedCollectiblesAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            try
            {
                int currentPage = page ?? 1;
                int currentPageSize = pageSize ?? 10;

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, CollectiblesIndexKey) ?? new();
                var published = new List<CollectiblesAdDto>();

                foreach (var key in index)
                {
                    try
                    {
                        var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                        if (state.ValueKind != JsonValueKind.Object) continue;

                        var subVertical = state.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                        var adUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : string.Empty;

                        if (!string.Equals(subVertical, "Collectibles", StringComparison.OrdinalIgnoreCase) || adUserId != userId)
                            continue;

                        var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var statusInt)
                            ? (AdStatus)statusInt : AdStatus.Draft;

                        if (status != AdStatus.Published && status != AdStatus.Approved)
                            continue;

                        var title = state.GetProperty("title").GetString();
                        if (!string.IsNullOrWhiteSpace(search) && !title.Contains(search, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var ad = new CollectiblesAdDto
                        {
                            Id = state.GetProperty("id").GetGuid(),
                            Title = state.GetProperty("title").GetString(),
                            Description = state.GetProperty("description").GetString(),
                            SubVertical = subVertical,
                            Category = state.GetProperty("category").GetString(),
                            L1Category = state.GetProperty("l1Category").GetString(),                            
                            L2Category = state.TryGetProperty("l2Category", out var l2) ? l2.GetString() : null,
                            Brand = state.TryGetProperty("brand", out var brand) ? brand.GetString() : null,
                            Price = state.GetProperty("price").GetDecimal(),
                            PriceType = state.GetProperty("priceType").GetString(),
                            Condition = state.GetProperty("condition").GetString(),
                            CountryOfOrigin = state.TryGetProperty("countryOfOrigin", out var origin) ? origin.GetString() : null,
                            Language = state.TryGetProperty("language", out var lang) ? lang.GetString() : null,
                            HasAuthenticityCertificate = state.TryGetProperty("hasAuthenticityCertificate", out var hasCert) && hasCert.GetBoolean(),
                            AuthenticityCertificateUrl = state.TryGetProperty("certificateUrl", out var certUrl) ? certUrl.GetString() ?? "" : "",
                            YearOrEra = state.TryGetProperty("yearOrEra", out var era) ? era.GetString() : null,
                            Rarity = state.TryGetProperty("rarity", out var rarity) ? rarity.GetString() : null,
                            Package = state.TryGetProperty("package", out var pkg) ? pkg.GetString() : null,
                            IsGraded = state.TryGetProperty("isGraded", out var graded) ? graded.GetBoolean() : null,
                            GradingCompany = state.TryGetProperty("gradingCompany", out var gradeCo) ? gradeCo.GetString() : null,
                            Grades = state.TryGetProperty("grades", out var grades) ? grades.GetString() : null,
                            Material = state.TryGetProperty("material", out var material) ? material.GetString() : null,
                            Scale = state.TryGetProperty("scale", out var scale) ? scale.GetString() : null,
                            SerialNumber = state.TryGetProperty("serialNumber", out var serial) ? serial.GetString() ?? "" : "",
                            Signed = state.TryGetProperty("signed", out var signed) ? signed.GetBoolean() : null,
                            SignedBy = state.TryGetProperty("signedBy", out var signedBy) ? signedBy.GetString() : null,
                            FramedBy = state.TryGetProperty("framedBy", out var framedBy) ? framedBy.GetString() : null,
                            ImageUrls = state.TryGetProperty("imageUrls", out var imgs) && imgs.ValueKind == JsonValueKind.Array
                            ? imgs.EnumerateArray().Select(img =>
                            {
                                return new ImageInfo
                                {
                                    AdImageFileNames = img.TryGetProperty("adImageFileNames", out var fn) ? fn.GetString() ?? "" : "",
                                    Url = img.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                                    Order = img.TryGetProperty("order", out var o) && o.TryGetInt32(out var ord) ? ord : 0
                                };
                            }).Where(i => !string.IsNullOrWhiteSpace(i.Url)).ToList(): new(),
                            PhoneNumber = state.TryGetProperty("phoneNumber", out var phone) ? phone.GetString() ?? "" : "",
                            WhatsAppNumber = state.TryGetProperty("whatsAppNumber", out var whatsapp) ? whatsapp.GetString() ?? "" : "",
                            ContactEmail = state.TryGetProperty("contactEmail", out var email) ? email.GetString() : "",
                            Location = state.TryGetProperty("location", out var loc) && loc.ValueKind == JsonValueKind.Array
                            ? loc.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrEmpty(x)).ToList()
                            : new(),
                            StreetNumber = state.TryGetProperty("streetNumber", out var street) ? street.GetString() ?? "" : "",
                            BuildingNumber = state.TryGetProperty("buildingNumber", out var building) ? building.GetString() : null,
                            HasWarranty = state.TryGetProperty("hasWarranty", out var warranty) && warranty.GetBoolean(),
                            IsHandmade = state.TryGetProperty("isHandmade", out var handmade) && handmade.GetBoolean(),
                            TearmsAndCondition = state.TryGetProperty("tearmsAndCondition", out var terms) && terms.GetBoolean(),
                            UserId = adUserId,
                            IsFeatured = state.TryGetProperty("isFeatured", out var featured) && featured.GetBoolean(),
                            IsPromoted = state.TryGetProperty("isPromoted", out var promoted) && promoted.GetBoolean(),
                            CreatedAt = state.GetProperty("createdAt").GetDateTime(),
                            Status = status,
                            RefreshCount = state.TryGetProperty("refreshCount", out var refreshCount) ? refreshCount.GetString() : "80",
                            ExpiryDate = state.TryGetProperty("expiryDate", out var expiryDate) ? expiryDate.GetDateTime() : DateTime.MinValue,
                            RefreshExpiry = state.TryGetProperty("refreshExpiry", out var refreshExpiryDate) ? refreshExpiryDate.GetDateTime() : DateTime.MinValue
                        };

                        published.Add(ad);
                    }
                    catch (Exception adEx)
                    {
                        _logger.LogError(adEx, "Error processing ad from key: {Key}", key);
                    }
                }

                published = sortOption switch
                {
                    AdSortOption.CreationDateOldest => published.OrderBy(a => a.CreatedAt).ToList(),
                    AdSortOption.CreationDateRecent => published.OrderByDescending(a => a.CreatedAt).ToList(),
                    AdSortOption.PriceHighToLow => published.OrderByDescending(a => a.Price).ToList(),
                    AdSortOption.PriceLowToHigh => published.OrderBy(a => a.Price).ToList(),
                    _ => published.OrderByDescending(a => a.CreatedAt).ToList()
                };

                var total = published.Count;
                var pagedItems = published
                                 .Skip((currentPage - 1) * currentPageSize)
                                 .Take(currentPageSize)
                                 .ToList();

                return new PaginatedCollectiblesAdResponseDto
                {
                    Total = total,
                    Items = pagedItems
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve user Collectibles ads", ex);
            }
        }

        public async Task<PaginatedCollectiblesAdResponseDto> GetUserUnPublishedCollectiblesAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            try
            {
                int currentPage = page ?? 1;
                int currentPageSize = pageSize ?? 10;

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, CollectiblesIndexKey) ?? new();
                var unpublished = new List<CollectiblesAdDto>();

                foreach (var key in index)
                {
                    try
                    {
                        var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                        if (state.ValueKind != JsonValueKind.Object) continue;

                        var subVertical = state.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                        var adUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : String.Empty;

                        if (!string.Equals(subVertical, "Collectibles", StringComparison.OrdinalIgnoreCase) || adUserId != userId)
                            continue;

                        var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var statusInt)
                            ? (AdStatus)statusInt : AdStatus.Draft;

                        var title = state.GetProperty("title").GetString();
                        if (!string.IsNullOrWhiteSpace(search) &&
                            (string.IsNullOrWhiteSpace(title) || !title.Contains(search, StringComparison.OrdinalIgnoreCase)))
                            continue;

                        if (status == AdStatus.Published || status == AdStatus.Approved)
                            continue;

                        var ad = new CollectiblesAdDto
                        {
                            Id = state.GetProperty("id").GetGuid(),
                            Title = state.GetProperty("title").GetString(),
                            Description = state.GetProperty("description").GetString(),
                            SubVertical = subVertical,
                            Category = state.GetProperty("category").GetString(),
                            L1Category = state.GetProperty("l1Category").GetString(),
                            L2Category = state.TryGetProperty("l2Category", out var l2) ? l2.GetString() : null,
                            Brand = state.TryGetProperty("brand", out var brand) ? brand.GetString() : null,
                            Price = state.GetProperty("price").GetDecimal(),
                            PriceType = state.GetProperty("priceType").GetString(),
                            Condition = state.GetProperty("condition").GetString(),
                            CountryOfOrigin = state.TryGetProperty("countryOfOrigin", out var origin) ? origin.GetString() : null,
                            Language = state.TryGetProperty("language", out var lang) ? lang.GetString() : null,
                            HasAuthenticityCertificate = state.TryGetProperty("hasAuthenticityCertificate", out var hasCert) && hasCert.GetBoolean(),
                            AuthenticityCertificateUrl = state.TryGetProperty("certificateUrl", out var certUrl) ? certUrl.GetString() ?? "" : "",
                            YearOrEra = state.TryGetProperty("yearOrEra", out var era) ? era.GetString() : null,
                            Rarity = state.TryGetProperty("rarity", out var rarity) ? rarity.GetString() : null,
                            Package = state.TryGetProperty("package", out var pkg) ? pkg.GetString() : null,
                            IsGraded = state.TryGetProperty("isGraded", out var graded) ? graded.GetBoolean() : null,
                            GradingCompany = state.TryGetProperty("gradingCompany", out var gradeCo) ? gradeCo.GetString() : null,
                            Grades = state.TryGetProperty("grades", out var grades) ? grades.GetString() : null,
                            Material = state.TryGetProperty("material", out var material) ? material.GetString() : null,
                            Scale = state.TryGetProperty("scale", out var scale) ? scale.GetString() : null,
                            SerialNumber = state.TryGetProperty("serialNumber", out var serial) ? serial.GetString() ?? "" : "",
                            Signed = state.TryGetProperty("signed", out var signed) ? signed.GetBoolean() : null,
                            SignedBy = state.TryGetProperty("signedBy", out var signedBy) ? signedBy.GetString() : null,
                            FramedBy = state.TryGetProperty("framedBy", out var framedBy) ? framedBy.GetString() : null,
                            ImageUrls = state.TryGetProperty("imageUrls", out var imgs) && imgs.ValueKind == JsonValueKind.Array
                            ? imgs.EnumerateArray().Select(img =>
                            {
                                return new ImageInfo
                                {
                                    AdImageFileNames = img.TryGetProperty("adImageFileNames", out var fn) ? fn.GetString() ?? "" : "",
                                    Url = img.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                                    Order = img.TryGetProperty("order", out var o) && o.TryGetInt32(out var ord) ? ord : 0
                                };
                            }).Where(i => !string.IsNullOrWhiteSpace(i.Url)).ToList() : new(),
                            PhoneNumber = state.TryGetProperty("phoneNumber", out var phone) ? phone.GetString() ?? "" : "",
                            WhatsAppNumber = state.TryGetProperty("whatsAppNumber", out var whatsapp) ? whatsapp.GetString() ?? "" : "",
                            ContactEmail = state.TryGetProperty("contactEmail", out var email) ? email.GetString() : "",
                            Location = state.TryGetProperty("location", out var loc) && loc.ValueKind == JsonValueKind.Array
                            ? loc.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrEmpty(x)).ToList()
                            : new(),
                            StreetNumber = state.TryGetProperty("streetNumber", out var street) ? street.GetString() ?? "" : "",
                            BuildingNumber = state.TryGetProperty("buildingNumber", out var building) ? building.GetString() : null,
                            HasWarranty = state.TryGetProperty("hasWarranty", out var warranty) && warranty.GetBoolean(),
                            IsHandmade = state.TryGetProperty("isHandmade", out var handmade) && handmade.GetBoolean(),
                            TearmsAndCondition = state.TryGetProperty("tearmsAndCondition", out var terms) && terms.GetBoolean(),
                            UserId = adUserId,
                            IsFeatured = state.TryGetProperty("isFeatured", out var featured) && featured.GetBoolean(),
                            IsPromoted = state.TryGetProperty("isPromoted", out var promoted) && promoted.GetBoolean(),
                            CreatedAt = state.GetProperty("createdAt").GetDateTime(),
                            Status = status,
                            RefreshCount = state.TryGetProperty("refreshCount", out var refreshCount) ? refreshCount.GetString() : "80",
                            ExpiryDate = state.TryGetProperty("expiryDate", out var expiryDate) ? expiryDate.GetDateTime() : DateTime.MinValue,
                            RefreshExpiry = state.TryGetProperty("refreshExpiry", out var refreshExpiryDate) ? refreshExpiryDate.GetDateTime() : DateTime.MinValue
                        };

                        unpublished.Add(ad);
                    }
                    catch (Exception adEx)
                    {
                        _logger.LogError(adEx, "Error processing ad from key: {Key}", key);
                    }
                }

                unpublished = sortOption switch
                {
                    AdSortOption.CreationDateOldest => unpublished.OrderBy(a => a.CreatedAt).ToList(),
                    AdSortOption.CreationDateRecent => unpublished.OrderByDescending(a => a.CreatedAt).ToList(),
                    AdSortOption.PriceHighToLow => unpublished.OrderByDescending(a => a.Price).ToList(),
                    AdSortOption.PriceLowToHigh => unpublished.OrderBy(a => a.Price).ToList(),
                    _ => unpublished.OrderByDescending(a => a.CreatedAt).ToList()
                };

                var total = unpublished.Count;
                var pagedItems = unpublished
                                 .Skip((currentPage - 1) * currentPageSize)
                                 .Take(currentPageSize)
                                 .ToList();

                return new PaginatedCollectiblesAdResponseDto
                {
                    Total = total,
                    Items = pagedItems
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve user Collectibles ads", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkPublishCollectiblesAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, CollectiblesIndexKey) ?? new();
                var failedAds = new List<Guid>();

                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    if (!index.Contains(key))
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    if (state.ValueKind != JsonValueKind.Object)
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var storedUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : String.Empty;
                    var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var val) ? (AdStatus)val : AdStatus.Draft;

                    if (storedUserId != userId || (status != AdStatus.Unpublished && status != AdStatus.Draft))
                    {
                        failedAds.Add(adId);
                    }
                }

                if (failedAds.Count > 0)
                {
                    return new BulkAdActionResponse
                    {
                        SuccessCount = 0,
                        FailedAdIds = failedAds,
                        Message = "Publish failed. Some ads are invalid."
                    };
                }

                int successCount = 0;
                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(state.ToString()!)!;
                    dict["status"] = (int)AdStatus.Published;

                    await _dapr.SaveStateAsync(UnifiedStore, key, dict, cancellationToken: cancellationToken);
                    successCount++;
                }

                return new BulkAdActionResponse
                {
                    SuccessCount = successCount,
                    FailedAdIds = new(),
                    Message = $"{successCount} ad(s) published successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while bulk publishing collectibles ads.");
                throw new InvalidOperationException("An unexpected error occurred while bulk publishing collectibles ads.", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkUnpublishCollectiblesAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, CollectiblesIndexKey) ?? new();
                var failedAds = new List<Guid>();

                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    if (!index.Contains(key))
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    if (state.ValueKind != JsonValueKind.Object)
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var storedUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : String.Empty;
                    var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var val) ? (AdStatus)val : AdStatus.Draft;

                    if (storedUserId != userId || status != AdStatus.Published)
                    {
                        failedAds.Add(adId);
                    }
                }

                if (failedAds.Count > 0)
                {
                    return new BulkAdActionResponse
                    {
                        SuccessCount = 0,
                        FailedAdIds = failedAds,
                        Message = "Unpublish failed. Some ads are invalid."
                    };
                }

                int successCount = 0;
                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(state.ToString()!)!;
                    dict["status"] = (int)AdStatus.Unpublished;

                    await _dapr.SaveStateAsync(UnifiedStore, key, dict, cancellationToken: cancellationToken);
                    successCount++;
                }

                return new BulkAdActionResponse
                {
                    SuccessCount = successCount,
                    FailedAdIds = new(),
                    Message = $"{successCount} ad(s) unpublished successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while bulk unpublishing collectibles ads.");
                throw new InvalidOperationException("An unexpected error occurred while bulk unpublishing collectibles ads.", ex);
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

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, indexKey) ?? new();
                var result = new List<CategoryTreeDto>();

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

    }
}
