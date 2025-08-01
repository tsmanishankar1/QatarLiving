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
using QLN.Common.Infrastructure.IService.ISearchService;
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

        public Task<bool> SaveSearch(SaveSearchRequestDto dto, string userId, CancellationToken cancellationToken = default)
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

        public async Task<ItemAdsAndDashboardResponse> GetUserItemsAdsWithDashboard(string userId, CancellationToken cancellationToken = default)
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

        public async Task<PrelovedAdsAndDashboardResponse> GetUserPrelovedAdsAndDashboard(string userId, CancellationToken cancellationToken = default)
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
            catch (Exception ex)
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

        public async Task<AdCreatedResponseDto> CreateClassifiedItemsAd(ClassifiedsItems dto, CancellationToken cancellationToken = default)
        {
           
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            if (dto.UserId == null) throw new ArgumentException("UserId is required.");

            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            if (dto.Images == null || dto.Images.Count == 0)
                throw new ArgumentException("Image URLs must be provided.");

            var adId = dto.Id != Guid.Empty ? dto.Id : throw new ArgumentException("Id must be provided");

            var key = $"ad-{adId}";

            try
            {
                var existing = await _dapr.GetStateAsync<object>(UnifiedStore, key);
                if (existing != null)
                {
                    throw new InvalidOperationException($"Ad with key {key} already exists.");
                }
                dto.Status = AdStatus.PendingApproval;

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, ItemsIndexKey) ?? new();
                index.Add(key);

                await _dapr.SaveStateAsync(UnifiedStore, key, dto);
                await _dapr.SaveStateAsync(UnifiedStore, ItemsIndexKey, index);
                var upsertRequest = await IndexItemsToAzureSearch(dto, cancellationToken);

                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ClassifiedsItemsIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }

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
        private async Task<CommonIndexRequest> IndexItemsToAzureSearch(ClassifiedsItems dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ClassifiedsItemsIndex
            {
                Id = dto.Id.ToString(),
                SubVertical = dto.SubVertical,
                AdType = dto.AdType.ToString(),
                Title = dto.Title,
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
                SubscriptionId = dto.SubscriptionId,
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
            return indexRequest;
        }
        private async Task<CommonIndexRequest> IndexPrelovedToAzureSearch(ClassifiedsPreloved dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ClassifiedsPrelovedIndex
            {
                Id = dto.Id.ToString(),
                SubscriptionId = dto.SubscriptionId,
                SubVertical = dto.SubVertical,
                AdType = dto.AdType.ToString(),
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                PriceType = dto.PriceType,
                CategoryId = dto.CategoryId,
                Category = dto.Category,
                L1CategoryId = dto.L1CategoryId,
                L1Category = dto.L1Category,
                L2CategoryId = dto.L2CategoryId,
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
                AttributesJson = JsonSerializer.Serialize(dto.Attributes ?? new Dictionary<string, string>()),

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
            return indexRequest;
        }
        private async Task<CommonIndexRequest> IndexCollectiblesToAzureSearch(ClassifiedsCollectibles dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ClassifiedsCollectiblesIndex
            {
                Id = dto.Id.ToString(),
                SubVertical = dto.SubVertical,
                SubscriptionId = dto.SubscriptionId,
                AdType = dto.AdType.ToString(),
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                PriceType = dto.PriceType,
                CategoryId = dto.CategoryId,
                Category = dto.Category,
                L1CategoryId = dto.L1CategoryId,
                L1Category = dto.L1Category,
                L2CategoryId = dto.L2CategoryId,
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
                AttributesJson = JsonSerializer.Serialize(dto.Attributes ?? new Dictionary<string, string>()),

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
            return indexRequest;
        }
        private async Task<CommonIndexRequest> IndexDealsToAzureSearch(ClassifiedsDeals dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ClassifiedsDealsIndex
            {
                Id = dto.Id.ToString(),
                Subvertical = dto.Subvertical,
                UserId = dto.UserId,
                BusinessName = dto.BusinessName,
                BranchNames = dto.BranchNames,
                BusinessType = dto.BusinessType,
                Title = dto.Title,
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
                ContactNumberCountryCode = dto.ContactNumberCountryCode,
                SubscriptionId = dto.SubscriptionId,
                WhatsappNumberCountryCode = dto.WhatsappNumberCountryCode,
                UpdatedAt = dto.UpdatedAt,
                UpdatedBy = dto.UpdatedBy,
                offertitle = dto.offertitle,
                ExpiryDate = dto.ExpiryDate,
                ImageUrl = dto.ImageUrl,
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
            return indexRequest;
        }

        public async Task<AdCreatedResponseDto> RefreshClassifiedItemsAd(SubVertical subVertical, Guid adId, CancellationToken cancellationToken)
        {
            try
            {
                object adItem = null;

                switch (subVertical)
                {
                    case SubVertical.Items:
                        adItem = await GetItemAdById(adId, cancellationToken);
                        break;
                    case SubVertical.Preloved:
                        adItem = await GetPrelovedAdById(adId, cancellationToken);
                        break;
                    case SubVertical.Collectibles:
                        adItem = await GetCollectiblesAdById(adId, cancellationToken);
                        break;
                    case SubVertical.Deals:
                        adItem = await GetDealsAdById(adId, cancellationToken);
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid SubVertical: {subVertical}");
                }

                if (adItem == null)
                {
                    _logger.LogError($"Ad with id {adId} not found in the {subVertical} vertical.");
                    throw new InvalidOperationException($"Ad with id {adId} not found.");
                }

                if (adItem is ClassifiedItems itemAd)
                {
                    itemAd.IsRefresh = true;
                    itemAd.CreatedDate = DateTime.UtcNow;
                    itemAd.RefreshExpiry = DateTime.UtcNow.AddHours(72);
                    await _dapr.SaveStateAsync(UnifiedStore, $"ad-{itemAd.Id}", itemAd);
                }
                else if (adItem is ClassifiedPreloved prelovedAd)
                {
                    prelovedAd.IsRefresh = true;
                    prelovedAd.CreatedDate = DateTime.UtcNow;
                    prelovedAd.RefreshExpiry = DateTime.UtcNow.AddHours(72);
                    await _dapr.SaveStateAsync(UnifiedStore, $"ad-{prelovedAd.Id}", prelovedAd);
                }
                else if (adItem is ClassifiedCollectibles collectiblesAd)
                {
                    collectiblesAd.IsRefresh = true;
                    collectiblesAd.CreatedDate = DateTime.UtcNow;
                    collectiblesAd.RefreshExpiry = DateTime.UtcNow.AddHours(72);
                    await _dapr.SaveStateAsync(UnifiedStore, $"ad-{collectiblesAd.Id}", collectiblesAd);
                }
                else if (adItem is ClassifiedDeals dealsAd)
                {
                    dealsAd.IsRefresh = true;
                    dealsAd.CreatedDate = DateTime.UtcNow;
                    dealsAd.RefreshExpiry = DateTime.UtcNow.AddHours(72);
                    await _dapr.SaveStateAsync(UnifiedStore, $"ad-{dealsAd.Id}", dealsAd);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported ad type: {adItem.GetType().Name}");
                }

                return new AdCreatedResponseDto
                {
                    AdId = adId,
                    Message = "Ad successfully refreshed."
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error occurred while refreshing ad.");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Ad not found or operation error while refreshing ad.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred while refreshing ad.");
                throw new InvalidOperationException("Failed to refresh the ad due to an unexpected error.", ex);
            }
        }

        public async Task<AdCreatedResponseDto> CreateClassifiedPrelovedAd(ClassifiedsPreloved dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (dto.Images == null || dto.Images.Count == 0)
                throw new ArgumentException("Image URLs must be provided.");
            if (string.IsNullOrWhiteSpace(dto.AuthenticityCertificateUrl))
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

                dto.Status = AdStatus.Draft;
                dto.CreatedAt = DateTime.UtcNow;

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, PrelovedIndexKey) ?? new();
                index.Add(key);

                await _dapr.SaveStateAsync(UnifiedStore, key, dto);
                await _dapr.SaveStateAsync(UnifiedStore, PrelovedIndexKey, index);
                var upsertRequest = await IndexPrelovedToAzureSearch(dto, cancellationToken);

                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ClassifiedsPrelovedIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }

                return new AdCreatedResponseDto
                {
                    AdId = adId,
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

        public async Task<AdCreatedResponseDto> CreateClassifiedCollectiblesAd(ClassifiedsCollectibles dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (dto.Images == null || dto.Images.Count == 0) throw new ArgumentException("Image URLs must be provided.");
            if (string.IsNullOrWhiteSpace(dto.AuthenticityCertificateUrl)) throw new ArgumentException("Certificate URL must be provided.");
            if (dto.Id == Guid.Empty) throw new ArgumentException("Id must be provided.");

            var adId = dto.Id;
            var key = $"ad-{adId}";

            try
            {
                var existing = await _dapr.GetStateAsync<object>(UnifiedStore, key);
                if (existing != null)
                    throw new InvalidOperationException($"Ad with key {key} already exists.");

                // Mutate necessary fields
                dto.Status = AdStatus.Draft;
                dto.CreatedAt = DateTime.UtcNow;

                // Update index
                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, CollectiblesIndexKey) ?? new();
                index.Add(key);

                // Save to state store
                await _dapr.SaveStateAsync(UnifiedStore, key, dto);
                await _dapr.SaveStateAsync(UnifiedStore, CollectiblesIndexKey, index);
                var upsertRequest = await IndexCollectiblesToAzureSearch(dto, cancellationToken);

                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ClassifiedsCollectiblesIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }

                return new AdCreatedResponseDto
                {
                    AdId = adId,
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

        public async Task<AdCreatedResponseDto> CreateClassifiedDealsAd(ClassifiedsDeals dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (dto.ImageUrl == null) throw new ArgumentException("Image URLs must be provided.");
            if (string.IsNullOrWhiteSpace(dto.FlyerFileUrl)) throw new ArgumentException("Flyer URL must be provided.");
            if (dto.Id == Guid.Empty) throw new ArgumentException("Id must be provided.");

            var adId = dto.Id;
            var key = $"ad-{adId}";

            try
            {
                var existing = await _dapr.GetStateAsync<object>(UnifiedStore, key);
                if (existing != null)
                    throw new InvalidOperationException($"Ad with key {key} already exists.");

                // Mutate necessary fields
                //dto.Status = AdStatus.Draft;
                dto.CreatedAt = DateTime.UtcNow;

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, DealsIndexKey) ?? new();
                index.Add(key);

                // Save to state store
                await _dapr.SaveStateAsync(UnifiedStore, key, dto);
                await _dapr.SaveStateAsync(UnifiedStore, DealsIndexKey, index);
                var upsertRequest = await IndexDealsToAzureSearch(dto, cancellationToken);

                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ClassifiedsDealsIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }

                return new AdCreatedResponseDto
                {
                    AdId = adId,
                    Title = dto.Title,
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
                            CertificateFileName = state.TryGetProperty("certificateFileName", out var certFileName) ? certFileName.GetString() ?? "" : "",
                            CertificateUrl = state.GetProperty("certificateUrl").GetString(),
                            ImageUrls = state.TryGetProperty("imageUrls", out var imgs) && imgs.ValueKind == JsonValueKind.Array
                            ? imgs.EnumerateArray().Select(img =>
                            {
                                return new ImageInfo
                                {
                                    Url = img.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                                    Order = img.TryGetProperty("order", out var o) && o.TryGetInt32(out var ord) ? ord : 0
                                };
                            }).Where(i => !string.IsNullOrEmpty(i.Url)).ToList()
                            : new List<ImageInfo>(),
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
                    catch (Exception ex)
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
            catch (Exception ex)
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
                            Id = state.TryGetProperty("id", out var id) ? id.GetGuid() : Guid.Empty,
                            Title = title,
                            SubVertical = subVertical,
                            Description = state.TryGetProperty("description", out var desc) ? desc.GetString() : string.Empty,
                            Category = state.TryGetProperty("category", out var cat) ? cat.GetString() : string.Empty,
                            L1Category = state.TryGetProperty("l1Category", out var l1Cat) ? l1Cat.GetString() : string.Empty,
                            L2Category = state.TryGetProperty("l2Category", out var l2Cat) ? l2Cat.GetString() : string.Empty,
                            Brand = state.TryGetProperty("brand", out var brand) ? brand.GetString() : string.Empty,
                            Model = state.TryGetProperty("model", out var model) ? model.GetString() : string.Empty,
                            Price = state.TryGetProperty("price", out var price) ? price.GetDecimal() : 0m,
                            PriceType = state.TryGetProperty("priceType", out var priceType) ? priceType.GetString() : string.Empty,
                            Condition = state.TryGetProperty("condition", out var condition) ? condition.GetString() : string.Empty,
                            Color = state.TryGetProperty("color", out var color) ? color.GetString() : string.Empty,
                            AcceptsOffers = state.TryGetProperty("acceptsOffers", out var offers) ? offers.GetString() : string.Empty,
                            MakeType = state.TryGetProperty("makeType", out var make) ? make.GetString() : string.Empty,
                            Capacity = state.TryGetProperty("capacity", out var capacity) ? capacity.GetString() : string.Empty,
                            Processor = state.TryGetProperty("processor", out var processor) ? processor.GetString() : string.Empty,
                            Coverage = state.TryGetProperty("coverage", out var coverage) ? coverage.GetString() : string.Empty,
                            Ram = state.TryGetProperty("ram", out var ram) ? ram.GetString() : string.Empty,
                            Resolution = state.TryGetProperty("resolution", out var res) ? res.GetString() : string.Empty,
                            BatteryPercentage = state.TryGetProperty("batteryPercentage", out var battery) ? battery.GetRawText() : string.Empty,
                            Size = state.TryGetProperty("size", out var size) ? size.GetString() : string.Empty,
                            SizeValue = state.TryGetProperty("sizeValue", out var sizeVal) ? sizeVal.GetString() : string.Empty,
                            Gender = state.TryGetProperty("gender", out var gender) ? gender.GetString() : string.Empty,
                            CertificateFileName = state.TryGetProperty("certificateFileName", out var certFileName) ? certFileName.GetString() : string.Empty,
                            CertificateUrl = state.TryGetProperty("certificateUrl", out var certUrl) ? certUrl.GetString() : string.Empty,
                            ImageUrls = state.TryGetProperty("imageUrls", out var imgs) && imgs.ValueKind == JsonValueKind.Array
                             ? imgs.EnumerateArray().Select(img =>
                             {
                                 return new ImageInfo
                                 {
                                     Url = img.TryGetProperty("url", out var u) ? u.GetString() : string.Empty,
                                     Order = img.TryGetProperty("order", out var o) && o.TryGetInt32(out var ord) ? ord : 0
                                 };
                             }).Where(i => !string.IsNullOrEmpty(i.Url)).ToList()
                             : new List<ImageInfo>(),
                            Phone = state.TryGetProperty("phoneNumber", out var phone) ? phone.GetString() : string.Empty,
                            WhatsAppNumber = state.TryGetProperty("whatsAppNumber", out var whatsapp) ? whatsapp.GetString() : string.Empty,
                            Zone = state.TryGetProperty("zone", out var zone) ? zone.GetString() : string.Empty,
                            StreetName = state.TryGetProperty("streetNumber", out var street) ? street.GetString() : string.Empty,
                            BuildingNumber = state.TryGetProperty("buildingNumber", out var building) ? building.GetString() : string.Empty,
                            Latitude = state.TryGetProperty("latitude", out var lat) ? lat.GetRawText() : string.Empty,
                            Longitude = state.TryGetProperty("longitude", out var lng) ? lng.GetRawText() : string.Empty,
                            CreatedAt = state.TryGetProperty("createdAt", out var createdAt) ? createdAt.GetDateTime() : DateTime.MinValue,
                            IsFeatured = state.TryGetProperty("isFeatured", out var featured) ? featured.GetBoolean() : false,
                            IsPromoted = state.TryGetProperty("isPromoted", out var promoted) ? promoted.GetBoolean() : false,
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

        public async Task<ClassifiedsItems> GetItemAdById(Guid adId, CancellationToken cancellationToken = default)
        {
            if (adId == Guid.Empty)
                throw new ArgumentException("Ad ID must not be empty.", nameof(adId));
            try
            {
                var key = $"ad-{adId}";

                var indexKeys = await _dapr.GetStateAsync<List<string>>(UnifiedStore, ItemsIndexKey) ?? new();

                if (!indexKeys.Contains(key))
                {
                    _logger.LogWarning("Ad ID {AdId} not found in active index. Possibly inactive or deleted.", adId);
                    return null;
                }

                var adItem = await _dapr.GetStateAsync<ClassifiedsItems>(UnifiedStore, key);

                if (adItem == null || !adItem.IsActive)
                {
                    _logger.LogWarning("Ad ID {AdId} is null or marked as inactive in state store.", adId);
                    return null;
                }

                return adItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching classified item details by adId: {AdId}", adId);
                throw new InvalidOperationException("Failed to fetch classified item ad by ID.", ex);
            }
        }

        public async Task<ClassifiedsPreloved> GetPrelovedAdById(Guid adId, CancellationToken cancellationToken = default)
        {
            if (adId == Guid.Empty)
                throw new ArgumentException("Ad ID must not be empty.", nameof(adId));

            try
            {
                var key = $"ad-{adId}";

                var indexKeys = await _dapr.GetStateAsync<List<string>>(UnifiedStore, PrelovedIndexKey) ?? new();

                if (!indexKeys.Contains(key))
                {
                    _logger.LogWarning("Ad ID {AdId} not found in Preloved index. Possibly inactive or deleted.", adId);
                    return null;
                }

                var adPreloved = await _dapr.GetStateAsync<ClassifiedsPreloved>(UnifiedStore, key);

                if (adPreloved == null || !adPreloved.IsActive)
                {
                    _logger.LogWarning("Ad ID {AdId} is null or marked as inactive in state store.", adId);
                    return null;
                }

                return adPreloved;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching classified preloved ad by ID: {AdId}", adId);
                throw new InvalidOperationException($"Failed to fetch classified preloved ad by ID {adId}.", ex);
            }
        }

        public async Task<ClassifiedsDeals> GetDealsAdById(Guid adId, CancellationToken cancellationToken = default)
        {
            if (adId == Guid.Empty)
                throw new ArgumentException("Ad ID must not be empty.", nameof(adId));
            try
            {
                var key = $"ad-{adId}";

                var indexKeys = await _dapr.GetStateAsync<List<string>>(UnifiedStore, DealsIndexKey) ?? new();

                if (!indexKeys.Contains(key))
                {
                    _logger.LogWarning("Ad ID {AdId} not found in Deals index. Possibly inactive or deleted.", adId);
                    return null;
                }

                var adDeals = await _dapr.GetStateAsync<ClassifiedsDeals>(UnifiedStore, key);

                if (adDeals == null || !adDeals.IsActive)
                {
                    _logger.LogWarning("Ad ID {AdId} is null or marked as inactive in state store.", adId);
                    return null;
                }

                return adDeals;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching classified deals details by adId: {AdId}", adId);
                throw new InvalidOperationException("Failed to fetch classified deals ad by ID.", ex);
            }
        }

        public async Task<ClassifiedsCollectibles> GetCollectiblesAdById(Guid adId, CancellationToken cancellationToken = default)
        {
            if (adId == Guid.Empty)
                throw new ArgumentException("Ad ID must not be empty.", nameof(adId));
            try
            {

                var key = $"ad-{adId}";

                var indexKeys = await _dapr.GetStateAsync<List<string>>(UnifiedStore, CollectiblesIndexKey) ?? new();

                if (!indexKeys.Contains(key))
                {
                    _logger.LogWarning("Ad ID {AdId} not found in Collectibles index. Possibly inactive or deleted.", adId);
                    return null;
                }

                var adCollectibles = await _dapr.GetStateAsync<ClassifiedsCollectibles>(UnifiedStore, key);

                if (adCollectibles == null || !adCollectibles.IsActive)
                {
                    _logger.LogWarning("Ad ID {AdId} is null or marked as inactive in state store.", adId);
                    return null;
                }

                return adCollectibles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching classified collectibles details by adId: {AdId}", adId);
                throw new InvalidOperationException("Failed to fetch classified collectibles ad by ID.", ex);
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
            catch (Exception ex)
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
                    dict["createdAt"] = DateTime.UtcNow;

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

                        var ad = new PrelovedAdDto
                        {
                            Id = state.TryGetProperty("id", out var idProp) ? idProp.GetGuid() : Guid.Empty,
                            Title = title,
                            SubVertical = subVertical,
                            Description = state.TryGetProperty("description", out var descProp) ? descProp.GetString() : string.Empty,
                            Category = state.TryGetProperty("category", out var categoryProp) ? categoryProp.GetString() : string.Empty,
                            L1Category = state.TryGetProperty("l1Category", out var l1CategoryProp) ? l1CategoryProp.GetString() : string.Empty,
                            L2Category = state.TryGetProperty("l2Category", out var l2CategoryProp) ? l2CategoryProp.GetString() : string.Empty,
                            Brand = state.TryGetProperty("brand", out var brandProp) ? brandProp.GetString() : string.Empty,
                            Model = state.TryGetProperty("model", out var modelProp) ? modelProp.GetString() : string.Empty,
                            Price = state.TryGetProperty("price", out var priceProp) ? priceProp.GetDecimal() : 0m,
                            PriceType = state.TryGetProperty("priceType", out var priceTypeProp) ? priceTypeProp.GetString() : string.Empty,
                            Condition = state.TryGetProperty("condition", out var conditionProp) ? conditionProp.GetString() : string.Empty,
                            Color = state.TryGetProperty("color", out var colorProp) ? colorProp.GetString() : string.Empty,
                            Capacity = state.TryGetProperty("capacity", out var capacityProp) ? capacityProp.GetString() : string.Empty,
                            Processor = state.TryGetProperty("processor", out var processorProp) ? processorProp.GetString() : string.Empty,
                            Coverage = state.TryGetProperty("coverage", out var coverageProp) ? coverageProp.GetString() : string.Empty,
                            Ram = state.TryGetProperty("ram", out var ramProp) ? ramProp.GetString() : string.Empty,
                            Resolution = state.TryGetProperty("resolution", out var resolutionProp) ? resolutionProp.GetString() : string.Empty,
                            BatteryPercentage = state.TryGetProperty("batteryPercentage", out var batteryProp) ? batteryProp.GetRawText() : string.Empty,
                            Size = state.TryGetProperty("size", out var sizeProp) ? sizeProp.GetString() : string.Empty,
                            SizeValue = state.TryGetProperty("sizeValue", out var sizeValueProp) ? sizeValueProp.GetString() : string.Empty,
                            Gender = state.TryGetProperty("gender", out var genderProp) ? genderProp.GetString() : string.Empty,
                            CertificateFileName = state.TryGetProperty("certificateFileName", out var certFileNameProp) ? certFileNameProp.GetString() : string.Empty,
                            CertificateUrl = state.TryGetProperty("certificateUrl", out var certUrlProp) ? certUrlProp.GetString() : string.Empty,
                            ImageUrls = state.TryGetProperty("imageUrls", out var imgsProp) && imgsProp.ValueKind == JsonValueKind.Array
                      ? imgsProp.EnumerateArray().Select(img =>
                      {
                          return new ImageInfo
                          {
                              Url = img.TryGetProperty("url", out var u) ? u.GetString() : string.Empty,
                              Order = img.TryGetProperty("order", out var o) && o.TryGetInt32(out var ord) ? ord : 0
                          };
                      }).Where(i => !string.IsNullOrEmpty(i.Url)).ToList()
                      : new List<ImageInfo>(),
                            PhoneNumber = state.TryGetProperty("phoneNumber", out var phoneProp) ? phoneProp.GetString() : string.Empty,
                            WhatsAppNumber = state.TryGetProperty("whatsAppNumber", out var whatsappProp) ? whatsappProp.GetString() : string.Empty,
                            Zone = state.TryGetProperty("zone", out var zoneProp) ? zoneProp.GetString() : string.Empty,
                            StreetNumber = state.TryGetProperty("streetNumber", out var streetProp) ? streetProp.GetString() : string.Empty,
                            BuildingNumber = state.TryGetProperty("buildingNumber", out var buildingProp) ? buildingProp.GetString() : string.Empty,
                            Latitude = state.TryGetProperty("latitude", out var latProp) ? latProp.GetRawText() : string.Empty,
                            Longitude = state.TryGetProperty("longitude", out var lngProp) ? lngProp.GetRawText() : string.Empty,
                            CreatedAt = state.TryGetProperty("createdAt", out var createdAtProp) ? createdAtProp.GetDateTime() : DateTime.MinValue,
                            IsFeatured = state.TryGetProperty("isFeatured", out var featuredProp) ? featuredProp.GetBoolean() : false,
                            IsPromoted = state.TryGetProperty("isPromoted", out var promotedProp) ? promotedProp.GetBoolean() : false,
                            Status = status,
                            UserId = adUserId,
                            RefreshCount = state.TryGetProperty("refreshCount", out var refreshCountProp) ? refreshCountProp.GetString() : "80",
                            ExpiryDate = state.TryGetProperty("expiryDate", out var expiryDateProp) ? expiryDateProp.GetDateTime() : DateTime.MinValue,
                            RefreshExpiry = state.TryGetProperty("refreshExpiry", out var refreshExpiryDateProp) ? refreshExpiryDateProp.GetDateTime() : DateTime.MinValue
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

                        var ad = new PrelovedAdDto
                        {
                            Id = state.TryGetProperty("id", out var idProp) ? idProp.GetGuid() : Guid.Empty,
                            Title = title,
                            SubVertical = subVertical,
                            Description = state.TryGetProperty("description", out var descProp) ? descProp.GetString() : string.Empty,
                            Category = state.TryGetProperty("category", out var categoryProp) ? categoryProp.GetString() : string.Empty,
                            L1Category = state.TryGetProperty("l1Category", out var l1CategoryProp) ? l1CategoryProp.GetString() : string.Empty,
                            L2Category = state.TryGetProperty("l2Category", out var l2CategoryProp) ? l2CategoryProp.GetString() : string.Empty,
                            Brand = state.TryGetProperty("brand", out var brandProp) ? brandProp.GetString() : string.Empty,
                            Model = state.TryGetProperty("model", out var modelProp) ? modelProp.GetString() : string.Empty,
                            Price = state.TryGetProperty("price", out var priceProp) ? priceProp.GetDecimal() : 0m,
                            PriceType = state.TryGetProperty("priceType", out var priceTypeProp) ? priceTypeProp.GetString() : string.Empty,
                            Condition = state.TryGetProperty("condition", out var conditionProp) ? conditionProp.GetString() : string.Empty,
                            Color = state.TryGetProperty("color", out var colorProp) ? colorProp.GetString() : string.Empty,
                            Capacity = state.TryGetProperty("capacity", out var capacityProp) ? capacityProp.GetString() : string.Empty,
                            Processor = state.TryGetProperty("processor", out var processorProp) ? processorProp.GetString() : string.Empty,
                            Coverage = state.TryGetProperty("coverage", out var coverageProp) ? coverageProp.GetString() : string.Empty,
                            Ram = state.TryGetProperty("ram", out var ramProp) ? ramProp.GetString() : string.Empty,
                            Resolution = state.TryGetProperty("resolution", out var resolutionProp) ? resolutionProp.GetString() : string.Empty,
                            BatteryPercentage = state.TryGetProperty("batteryPercentage", out var batteryProp) ? batteryProp.GetRawText() : string.Empty,
                            Size = state.TryGetProperty("size", out var sizeProp) ? sizeProp.GetString() : string.Empty,
                            SizeValue = state.TryGetProperty("sizeValue", out var sizeValueProp) ? sizeValueProp.GetString() : string.Empty,
                            Gender = state.TryGetProperty("gender", out var genderProp) ? genderProp.GetString() : string.Empty,
                            CertificateFileName = state.TryGetProperty("certificateFileName", out var certFileNameProp) ? certFileNameProp.GetString() : string.Empty,
                            CertificateUrl = state.TryGetProperty("certificateUrl", out var certUrlProp) ? certUrlProp.GetString() : string.Empty,
                            ImageUrls = state.TryGetProperty("imageUrls", out var imgsProp) && imgsProp.ValueKind == JsonValueKind.Array
                        ? imgsProp.EnumerateArray().Select(img =>
                        {
                            return new ImageInfo
                            {
                                Url = img.TryGetProperty("url", out var u) ? u.GetString() : string.Empty,
                                Order = img.TryGetProperty("order", out var o) && o.TryGetInt32(out var ord) ? ord : 0
                            };
                        }).Where(i => !string.IsNullOrEmpty(i.Url)).ToList()
                        : new List<ImageInfo>(),
                            PhoneNumber = state.TryGetProperty("phoneNumber", out var phoneProp) ? phoneProp.GetString() : string.Empty,
                            WhatsAppNumber = state.TryGetProperty("whatsAppNumber", out var whatsappProp) ? whatsappProp.GetString() : string.Empty,
                            Zone = state.TryGetProperty("zone", out var zoneProp) ? zoneProp.GetString() : string.Empty,
                            StreetNumber = state.TryGetProperty("streetNumber", out var streetProp) ? streetProp.GetString() : string.Empty,
                            BuildingNumber = state.TryGetProperty("buildingNumber", out var buildingProp) ? buildingProp.GetString() : string.Empty,
                            Latitude = state.TryGetProperty("latitude", out var latProp) ? latProp.GetRawText() : string.Empty,
                            Longitude = state.TryGetProperty("longitude", out var lngProp) ? lngProp.GetRawText() : string.Empty,
                            CreatedAt = state.TryGetProperty("createdAt", out var createdAtProp) ? createdAtProp.GetDateTime() : DateTime.MinValue,
                            IsFeatured = state.TryGetProperty("isFeatured", out var featuredProp) ? featuredProp.GetBoolean() : false,
                            IsPromoted = state.TryGetProperty("isPromoted", out var promotedProp) ? promotedProp.GetBoolean() : false,
                            Status = status,
                            UserId = adUserId,
                            RefreshCount = state.TryGetProperty("refreshCount", out var refreshCountProp) ? refreshCountProp.GetString() : "80",
                            ExpiryDate = state.TryGetProperty("expiryDate", out var expiryDateProp) ? expiryDateProp.GetDateTime() : DateTime.MinValue,
                            RefreshExpiry = state.TryGetProperty("refreshExpiry", out var refreshExpiryDateProp) ? refreshExpiryDateProp.GetDateTime() : DateTime.MinValue
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
                    dict["createdAt"] = DateTime.UtcNow;

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

                        var ad = new DealsAdDto
                        {
                            Id = state.GetProperty("id").GetGuid(),
                            Title = state.GetProperty("title").GetString(),
                            SubVertical = subVertical ?? "Deals",
                            FlyerName = state.TryGetProperty("flyerName", out var flyerName) ? flyerName.GetString() ?? "" : "",
                            FlyerFile = state.TryGetProperty("flyerFile", out var flyer) ? flyer.GetString() ?? "" : "",
                            ImageUrl = state.TryGetProperty("imageUrl", out var imgs) && imgs.ValueKind == JsonValueKind.Array
                            ? imgs.EnumerateArray().Select(img =>
                            {
                                return new ImageInfo
                                {
                                    Url = img.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                                    Order = img.TryGetProperty("order", out var o) && o.TryGetInt32(out var ord) ? ord : 0
                                };
                            }).Where(i => !string.IsNullOrWhiteSpace(i.Url)).ToList() : new(),
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
                    catch (Exception ex)
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
                        var ad = new DealsAdDto
                        {
                            Id = state.GetProperty("id").GetGuid(),
                            Title = state.GetProperty("title").GetString(),
                            SubVertical = subVertical ?? "Deals",
                            FlyerName = state.TryGetProperty("flyerName", out var flyerName) ? flyerName.GetString() ?? "" : "",
                            FlyerFile = state.TryGetProperty("flyerFile", out var flyer) ? flyer.GetString() ?? "" : "",
                            ImageUrl = state.TryGetProperty("imageUrl", out var imgs) && imgs.ValueKind == JsonValueKind.Array
                            ? imgs.EnumerateArray().Select(img =>
                            {
                                return new ImageInfo
                                {
                                    Url = img.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                                    Order = img.TryGetProperty("order", out var o) && o.TryGetInt32(out var ord) ? ord : 0
                                };
                            }).Where(i => !string.IsNullOrWhiteSpace(i.Url)).ToList() : new(),
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
                    catch (Exception ex)
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
                    dict["createdAt"] = DateTime.UtcNow;

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
                            CertificateFileName = state.TryGetProperty("certificateFileName", out var certFileName) ? certFileName.GetString() ?? "" : "",
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
                            CertificateFileName = state.TryGetProperty("certificateFileName", out var certFileName) ? certFileName.GetString() ?? "" : "",
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
                    dict["createdAt"] = DateTime.UtcNow;

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

        public async Task<AdUpdatedResponseDto> UpdateClassifiedItemsAd(ClassifiedsItems dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            var key = $"ad-{dto.Id}";

            try
            {
                var existingAd = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);
                if (existingAd.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException($"Ad with key {key} does not exist.");
                }

                if (!string.Equals(dto.SubVertical, "Items", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("This service only supports updating ads under the 'Items' vertical.");
                }
                await _dapr.SaveStateAsync(UnifiedStore, key, dto);

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, ItemsIndexKey) ?? new();
                if (!index.Contains(key))
                {
                    index.Add(key);
                }

                await _dapr.SaveStateAsync(UnifiedStore, ItemsIndexKey, index);
                var upsertRequest = await IndexItemsToAzureSearch(dto, cancellationToken);

                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ClassifiedsItemsIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }

                return new AdUpdatedResponseDto
                {
                    AdId = dto.Id,
                    Title = dto.Title ?? existingAd.GetProperty("title").GetString(),
                    UpdatedAt = DateTime.UtcNow,
                    Message = "Items Ad updated successfully"
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Ad not found or conflict occurred during update.");
                throw new InvalidOperationException("Ad does not exist or conflict occurred.", ex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed during ad update.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during ad update.");
                throw new InvalidOperationException("An unexpected error occurred while updating the ad. Please try again later.", ex);
            }
        }

        public async Task<AdUpdatedResponseDto> UpdateClassifiedPrelovedAd(ClassifiedsPreloved dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            var key = $"ad-{dto.Id}";

            try
            {
                var existingAd = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);

                if (existingAd.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException($"Ad with key {key} does not exist.");
                }

                if (!string.Equals(dto.SubVertical, "Preloved", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("This service only supports updating ads under the 'Preloved' vertical.");
                }

                await _dapr.SaveStateAsync(UnifiedStore, key, dto);

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, PrelovedIndexKey) ?? new();
                if (!index.Contains(key))
                {
                    index.Add(key);
                }

                await _dapr.SaveStateAsync(UnifiedStore, PrelovedIndexKey, index);
                var upsertRequest = await IndexPrelovedToAzureSearch(dto, cancellationToken);

                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ClassifiedsPrelovedIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }

                return new AdUpdatedResponseDto
                {
                    AdId = dto.Id,
                    Title = dto.Title ?? existingAd.GetProperty("title").GetString(),
                    UpdatedAt = DateTime.UtcNow,
                    Message = "Preloved Ad updated successfully"
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Ad not found or conflict occurred during update.");
                throw new InvalidOperationException("Ad does not exist or conflict occurred.", ex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed during ad update.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during ad update.");
                throw new InvalidOperationException("An unexpected error occurred while updating the ad. Please try again later.", ex);
            }
        }

        public async Task<AdUpdatedResponseDto> UpdateClassifiedCollectiblesAd(ClassifiedsCollectibles dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            var key = $"ad-{dto.Id}";

            try
            {
                var existingAd = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);

                if (existingAd.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException($"Ad with key {key} does not exist.");
                }

                if (!string.Equals(dto.SubVertical, "Collectibles", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("This service only supports updating ads under the 'Collectibles' vertical.");
                }

                await _dapr.SaveStateAsync(UnifiedStore, key, dto);

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, CollectiblesIndexKey) ?? new();
                if (!index.Contains(key))
                {
                    index.Add(key);
                }

                await _dapr.SaveStateAsync(UnifiedStore, CollectiblesIndexKey, index);
                var upsertRequest = await IndexCollectiblesToAzureSearch(dto, cancellationToken);

                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ClassifiedsCollectiblesIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }

                return new AdUpdatedResponseDto
                {
                    AdId = dto.Id,
                    Title = dto.Title ?? existingAd.GetProperty("title").GetString(),
                    UpdatedAt = DateTime.UtcNow,
                    Message = "Collectibles Ad updated successfully"
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Ad not found or conflict occurred during update.");
                throw new InvalidOperationException("Ad does not exist or conflict occurred.", ex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed during ad update.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during ad update.");
                throw new InvalidOperationException("An unexpected error occurred while updating the ad. Please try again later.", ex);
            }
        }

        public async Task<AdUpdatedResponseDto> UpdateClassifiedDealsAd(ClassifiedsDeals dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            var key = $"ad-{dto.Id}";

            try
            {
                var existingAd = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);

                if (existingAd.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException($"Ad with key {key} does not exist.");
                }

                if (!string.Equals(dto.Subvertical, "Deals", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("This service only supports updating ads under the 'Deals' vertical.");
                }

                await _dapr.SaveStateAsync(UnifiedStore, key, dto);

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, DealsIndexKey) ?? new();
                if (!index.Contains(key))
                {
                    index.Add(key);
                }

                await _dapr.SaveStateAsync(UnifiedStore, DealsIndexKey, index);
                var upsertRequest = await IndexDealsToAzureSearch(dto, cancellationToken);

                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ClassifiedsDealsIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }

                return new AdUpdatedResponseDto
                {
                    AdId = dto.Id,
                    Title = dto.Title ?? existingAd.GetProperty("title").GetString(),
                    UpdatedAt = DateTime.UtcNow,
                    Message = "Deals Ad updated successfully"
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Ad not found or conflict occurred during update.");
                throw new InvalidOperationException("Ad does not exist or conflict occurred.", ex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed during ad update.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during ad update.");
                throw new InvalidOperationException("An unexpected error occurred while updating the ad. Please try again later.", ex);
            }
        }


    }
}
