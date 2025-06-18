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

        private const string UnifiedStore = "adstore";
        private const string UnifiedIndexKey = "ad-index";
        private const string ItemsIndexKey = "items-ad-index";
        private const string PrelovedIndexKey = "preloved-index";
        private const string CollectiblesIndexKey = "collectibles-index";
        private const string DealsIndexKey = "deals-index";
        private const string ItemsCategoryIndexKey = "items-category-index";
        private const string PrelovedCategoryIndexKey = "preloved-category-index";
        private const string CollectiblesCategoryIndexKey = "collectibles-category-index";
        private const string DealsCategoryIndexKey = "deals-category-index";


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

            if (dto.UserId == Guid.Empty) throw new ArgumentException("UserId is required.");

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
                    dto.Category,
                    dto.SubCategory,
                    dto.L2Category,
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
                    Status = AdStatus.Draft
                };
                
                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, ItemsIndexKey) ?? new();
                index.Add(key);

                await _dapr.SaveStateAsync(UnifiedStore, key, adItem);
                await _dapr.SaveStateAsync(UnifiedStore, ItemsIndexKey, index);

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

            if (dto.UserId == Guid.Empty) throw new ArgumentException("UserId is required.");

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
                    dto.Category,
                    dto.SubCategory,
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
                    Status = AdStatus.Draft
                };

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, PrelovedIndexKey) ?? new();
                index.Add(key);

                await _dapr.SaveStateAsync(UnifiedStore, key, adItem);
                await _dapr.SaveStateAsync(UnifiedStore, PrelovedIndexKey, index);

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

            if (dto.UserId == Guid.Empty) throw new ArgumentException("UserId is required.");

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
                    dto.Category,
                    dto.SubCategory,
                    dto.L2Category,
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
                    CreatedAt = DateTime.UtcNow,
                    Status = AdStatus.Draft
                };

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, CollectiblesIndexKey) ?? new();
                index.Add(key);

                await _dapr.SaveStateAsync(UnifiedStore, key, adItem);
                await _dapr.SaveStateAsync(UnifiedStore, UnifiedIndexKey, index);

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

            if (dto.UserId == Guid.Empty) throw new ArgumentException("UserId is required.");

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
                    Status = AdStatus.Draft
                };

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, DealsIndexKey) ?? new();
                index.Add(key);

                await _dapr.SaveStateAsync(UnifiedStore, key, adItem);
                await _dapr.SaveStateAsync(UnifiedStore, DealsIndexKey, index);


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

        public async Task<ItemAdListDto> GetUserItemsAd(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, ItemsIndexKey) ?? new();

                var list = new ItemAdListDto();

                foreach (var key in index)
                {
                    try
                    {
                        var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);

                        if (state.ValueKind != JsonValueKind.Object) continue;

                        var subVertical = state.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                        var adUserId = state.TryGetProperty("userId", out var uid) ? uid.GetGuid() : Guid.Empty;

                        if (!string.Equals(subVertical, "Items", StringComparison.OrdinalIgnoreCase) || adUserId != userId)
                            continue;

                        var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var statusInt)
                            ? (AdStatus)statusInt : AdStatus.Draft;

                        var ad = new ItemAdDto
                        {
                            Id = state.GetProperty("id").GetGuid(),
                            Title = state.GetProperty("title").GetString(),
                            SubVertical = subVertical,
                            Description = state.GetProperty("description").GetString(),
                            Category = state.GetProperty("category").GetString(),
                            SubCategory = state.GetProperty("subCategory").GetString(),
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
                            UserId = adUserId
                        };

                        if (status == AdStatus.Published || status == AdStatus.Approved)
                            list.PublishedAds.Add(ad);
                        else
                            list.UnpublishedAds.Add(ad);
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, "Error processing ad from key: {Key}", key);
                    }
                }

                return list;
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve user items ads", ex);
            }
        }

        public async Task<PrelovedAdListDto> GetUserPrelovedAds(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, PrelovedIndexKey) ?? new();
                var list = new PrelovedAdListDto();

                foreach (var key in index)
                {
                    try
                    {
                        var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);
                        if (state.ValueKind != JsonValueKind.Object) continue;

                        var subVertical = state.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                        var adUserId = state.TryGetProperty("userId", out var uid) ? uid.GetGuid() : Guid.Empty;

                        if (!string.Equals(subVertical, "Preloved", StringComparison.OrdinalIgnoreCase) || adUserId != userId)
                            continue;

                        var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var statusInt)
                            ? (AdStatus)statusInt : AdStatus.Draft;

                        var ad = new PrelovedAdDto
                        {
                            Id = state.GetProperty("id").GetGuid(),
                            Title = state.GetProperty("title").GetString(),
                            SubVertical = subVertical,
                            Description = state.GetProperty("description").GetString(),
                            Category = state.GetProperty("category").GetString(),
                            SubCategory = state.GetProperty("subCategory").GetString(),
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
                            UserId = adUserId
                        };

                        if (status == AdStatus.Published || status == AdStatus.Approved)
                            list.PublishedAds.Add(ad);
                        else
                            list.UnpublishedAds.Add(ad);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing ad from key: {Key}", key); 
                    }
                    
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve user Preloved ads", ex);
            }
        }

        public async Task<DealsAdListDto> GetUserDealsAds(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, DealsIndexKey) ?? new();
                var list = new DealsAdListDto();

                foreach (var key in index)
                {
                    try
                    {
                        var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                        if (state.ValueKind != JsonValueKind.Object) continue;

                        var subVertical = state.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                        var adUserId = state.TryGetProperty("userId", out var uid) ? uid.GetGuid() : Guid.Empty;

                        if (!string.Equals(subVertical, "Deals", StringComparison.OrdinalIgnoreCase) || adUserId != userId)
                            continue;

                        var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var statusInt)
                            ? (AdStatus)statusInt : AdStatus.Draft;

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
                            Status = status
                        };

                        if (status == AdStatus.Published || status == AdStatus.Approved)
                            list.PublishedAds.Add(ad);
                        else
                            list.UnpublishedAds.Add(ad);
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, "Error processing ad from key: {Key}", key);
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve user Deals ads", ex);
            }
        }

        public async Task<CollectiblesAdListDto> GetUserCollectiblesAds(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, CollectiblesIndexKey) ?? new();
                var list = new CollectiblesAdListDto();

                foreach (var key in index)
                {
                    try
                    {
                        var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key);
                        if (state.ValueKind != JsonValueKind.Object) continue;

                        var subVertical = state.TryGetProperty("subVertical", out var sv) ? sv.GetString() : null;
                        var adUserId = state.TryGetProperty("userId", out var uid) ? uid.GetGuid() : Guid.Empty;

                        if (!string.Equals(subVertical, "Collectibles", StringComparison.OrdinalIgnoreCase) || adUserId != userId)
                            continue;

                        var status = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var statusInt)
                            ? (AdStatus)statusInt : AdStatus.Draft;

                        var ad = new CollectiblesAdDto
                        {
                            Id = state.GetProperty("id").GetGuid(),
                            Title = state.GetProperty("title").GetString(),
                            Description = state.GetProperty("description").GetString(),
                            SubVertical = subVertical,
                            Category = state.GetProperty("category").GetString(),
                            SubCategory = state.GetProperty("subCategory").GetString(),
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
                            Status = status
                        };

                        if (status == AdStatus.Published || status == AdStatus.Approved)
                            list.PublishedAds.Add(ad);
                        else
                            list.UnpublishedAds.Add(ad);
                    }
                    catch (Exception adEx)
                    {
                        _logger.LogError(adEx, "Error processing ad from key: {Key}", key);
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve user Collectibles ads", ex);
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
    }
}
