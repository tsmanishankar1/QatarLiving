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
using QLN.Common.Infrastructure.Service.FileStorage;
using QLN.Common.Infrastructure.Utilities;
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
                await IndexItemsToAzureSearch(dto, cancellationToken);

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

                if (adItem is ClassifiedsItems itemAd)
                {
                    itemAd.CreatedAt = DateTime.UtcNow;
                    itemAd.LastRefreshedOn = DateTime.UtcNow.AddHours(72);
                    await _dapr.SaveStateAsync(UnifiedStore, $"ad-{itemAd.Id}", itemAd);
                }
                else if (adItem is ClassifiedsPreloved prelovedAd)
                {
                    prelovedAd.CreatedAt = DateTime.UtcNow;
                    prelovedAd.LastRefreshedOn = DateTime.UtcNow.AddHours(72);
                    await _dapr.SaveStateAsync(UnifiedStore, $"ad-{prelovedAd.Id}", prelovedAd);
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
                await IndexPrelovedToAzureSearch(dto, cancellationToken);

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
            if (string.IsNullOrWhiteSpace(dto.AuthenticityCertificateUrl) && dto.HasAuthenticityCertificate) throw new ArgumentException("Certificate URL must be provided.");
            if (dto.Id == Guid.Empty) throw new ArgumentException("Id must be provided.");

            var adId = dto.Id;
            var key = $"ad-{adId}";

            try
            {
                var existing = await _dapr.GetStateAsync<object>(UnifiedStore, key);
                if (existing != null)
                    throw new InvalidOperationException($"Ad with key {key} already exists.");

                dto.Status = AdStatus.Draft;
                dto.CreatedAt = DateTime.UtcNow;

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, CollectiblesIndexKey) ?? new();
                index.Add(key);

                await _dapr.SaveStateAsync(UnifiedStore, key, dto);
                await _dapr.SaveStateAsync(UnifiedStore, CollectiblesIndexKey, index);
                await IndexCollectiblesToAzureSearch(dto, cancellationToken);
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
                //dto.Status = AdStatus.Draft;
                dto.CreatedAt = DateTime.UtcNow;

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, DealsIndexKey) ?? new();
                index.Add(key);

                await _dapr.SaveStateAsync(UnifiedStore, key, dto);
                await _dapr.SaveStateAsync(UnifiedStore, DealsIndexKey, index);
                await IndexDealsToAzureSearch(dto, cancellationToken);
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
                    throw new KeyNotFoundException($"Ad with key {adId} does not exist.");
                }

                var adItem = await _dapr.GetStateAsync<ClassifiedsItems>(UnifiedStore, key);

                if (adItem == null || !adItem.IsActive)
                {
                    _logger.LogWarning("Ad ID {AdId} is null or marked as inactive in state store.", adId);
                    throw new KeyNotFoundException($"Ad with key {adId} does not exist.");
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
                    throw new KeyNotFoundException($"Ad with key {adId} does not exist.");
                }

                var adPreloved = await _dapr.GetStateAsync<ClassifiedsPreloved>(UnifiedStore, key);

                if (adPreloved == null || !adPreloved.IsActive)
                {
                    _logger.LogWarning("Ad ID {AdId} is null or marked as inactive in state store.", adId);
                    throw new KeyNotFoundException($"Ad with key {adId} does not exist.");
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
                    throw new KeyNotFoundException($"Ad with key {adId} does not exist.");
                }

                var adDeals = await _dapr.GetStateAsync<ClassifiedsDeals>(UnifiedStore, key);

                if (adDeals == null || !adDeals.IsActive)
                {
                    _logger.LogWarning("Ad ID {AdId} is null or marked as inactive in state store.", adId);
                    throw new KeyNotFoundException($"Ad with key {adId} does not exist.");
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
                    throw new KeyNotFoundException($"Ad with key {adId} does not exist.");
                }

                var adCollectibles = await _dapr.GetStateAsync<ClassifiedsCollectibles>(UnifiedStore, key);

                if (adCollectibles == null || !adCollectibles.IsActive)
                {
                    _logger.LogWarning("Ad ID {AdId} is null or marked as inactive in state store.", adId);
                    throw new KeyNotFoundException($"Ad with key {adId} does not exist.");
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
            if (dto.UpdatedBy == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            var key = $"ad-{dto.Id}";

            try
            {
                var existingAd = await GetItemAdById(dto.Id, cancellationToken);
                if (existingAd == null)
                    throw new KeyNotFoundException($"Ad with key {key} does not exist.");

                if (!string.Equals(dto.SubVertical, "Items", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("This service only supports updating ads under the 'Items' vertical.");

                AdUpdateHelper.ApplySelectiveUpdates(existingAd, dto);

                await _dapr.SaveStateAsync(UnifiedStore, key, existingAd);

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, ItemsIndexKey) ?? new();
                if (!index.Contains(key))
                {
                    index.Add(key);
                    await _dapr.SaveStateAsync(UnifiedStore, ItemsIndexKey, index);
                }

                await IndexItemsToAzureSearch(existingAd, cancellationToken);

                return new AdUpdatedResponseDto
                {
                    AdId = existingAd.Id,
                    Title = existingAd.Title,
                    UpdatedAt = DateTime.UtcNow,
                    Message = "Items Ad updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Items Ad.");
                throw new InvalidOperationException("Failed to update Items ad.", ex);
            }
        }

        public async Task<AdUpdatedResponseDto> UpdateClassifiedPrelovedAd(ClassifiedsPreloved dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.UpdatedBy == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            var key = $"ad-{dto.Id}";

            try
            {
                var existingAd = await GetPrelovedAdById(dto.Id, cancellationToken);
                if (existingAd == null)
                    throw new KeyNotFoundException($"Ad with key {key} does not exist.");

                if (!string.Equals(dto.SubVertical, "Preloved", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("This service only supports updating ads under the 'Preloved' vertical.");

                AdUpdateHelper.ApplySelectiveUpdates(existingAd, dto);

                await _dapr.SaveStateAsync(UnifiedStore, key, existingAd);

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, PrelovedIndexKey) ?? new();
                if (!index.Contains(key))
                {
                    index.Add(key);
                    await _dapr.SaveStateAsync(UnifiedStore, PrelovedIndexKey, index);
                }

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

        public async Task<AdUpdatedResponseDto> UpdateClassifiedCollectiblesAd(ClassifiedsCollectibles dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.UpdatedBy == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            var key = $"ad-{dto.Id}";

            try
            {
                var existingAd = await GetCollectiblesAdById(dto.Id, cancellationToken);
                if (existingAd == null)
                    throw new KeyNotFoundException($"Ad with key {key} does not exist.");

                if (!string.Equals(dto.SubVertical, "Collectibles", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("This service only supports updating ads under the 'Collectibles' vertical.");

                AdUpdateHelper.ApplySelectiveUpdates(existingAd, dto);

                await _dapr.SaveStateAsync(UnifiedStore, key, existingAd);

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, CollectiblesIndexKey) ?? new();
                if (!index.Contains(key))
                {
                    index.Add(key);
                    await _dapr.SaveStateAsync(UnifiedStore, CollectiblesIndexKey, index);
                }

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

        public async Task<AdUpdatedResponseDto> UpdateClassifiedDealsAd(ClassifiedsDeals dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.UpdatedBy == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");

            var key = $"ad-{dto.Id}";

            try
            {
                var existingAd = await GetDealsAdById(dto.Id, cancellationToken);
                if (existingAd == null)
                    throw new KeyNotFoundException($"Ad with key {key} does not exist.");

                if (!string.Equals(dto.Subvertical, "Deals", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("This service only supports updating ads under the 'Deals' vertical.");
                AdUpdateHelper.ApplySelectiveUpdates(existingAd, dto);

                await _dapr.SaveStateAsync(UnifiedStore, key, existingAd);

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, DealsIndexKey) ?? new();
                if (!index.Contains(key))
                {
                    index.Add(key);
                    await _dapr.SaveStateAsync(UnifiedStore, DealsIndexKey, index);
                }

                await IndexDealsToAzureSearch(existingAd, cancellationToken);

                return new AdUpdatedResponseDto
                {
                    AdId = existingAd.Id,
                    Title = existingAd.Title,
                    UpdatedAt = DateTime.UtcNow,
                    Message = "Deals Ad updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Deals Ad.");
                throw new InvalidOperationException("Failed to update Deals ad.", ex);
            }
        }

        public async Task<PaginatedAdResponseDto> GetFilteredAds(string subVertical,bool? isPublished,int page,int pageSize,string? search,string userId,CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetFilteredAds started | SubVertical: {SubVertical}, IsPublished: {IsPublished}, Page: {Page}, PageSize: {PageSize}, UserId: {UserId}, Search: {Search}",
                subVertical, isPublished, page, pageSize, userId, search);

            var normalizedSubVertical = subVertical.ToLowerInvariant();

            var indexKey = normalizedSubVertical switch
            {
                "items" => ConstantValues.StateStoreNames.ItemsIndexKey,
                "preloved" => ConstantValues.StateStoreNames.PrelovedIndexKey,
                "deals" => ConstantValues.StateStoreNames.DealsIndexKey,
                "collectibles" => ConstantValues.StateStoreNames.CollectiblesIndexKey,
                _ => throw new ArgumentException("Invalid subVertical", nameof(subVertical))
            };

            _logger.LogDebug("Using index key: {IndexKey}", indexKey);

            var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, indexKey) ?? new();
            _logger.LogInformation("Fetched {Count} keys from state store", index.Count);

            List<object> results = new();

            foreach (var key in index)
            {
                try
                {
                    object? ad = normalizedSubVertical switch
                    {
                        "items" => await _dapr.GetStateAsync<ClassifiedsItems>(UnifiedStore, key),
                        "preloved" => await _dapr.GetStateAsync<ClassifiedsPreloved>(UnifiedStore, key),
                        "deals" => await _dapr.GetStateAsync<ClassifiedsDeals>(UnifiedStore, key),
                        "collectibles" => await _dapr.GetStateAsync<ClassifiedsCollectibles>(UnifiedStore, key),
                        _ => null
                    };

                    if (ad == null)
                    {
                        _logger.LogWarning("Ad not found or null for key: {Key}", key);
                        continue;
                    }

                    dynamic adDynamic = ad;

                    if (!string.Equals(adDynamic.UserId, userId, StringComparison.OrdinalIgnoreCase)) continue;

                    if (!string.Equals(adDynamic.SubVertical, subVertical, StringComparison.OrdinalIgnoreCase)) continue;

                    if (isPublished == true &&
                        adDynamic.Status != AdStatus.Published &&
                        adDynamic.Status != AdStatus.Approved)
                        continue;

                    if (isPublished == false &&
                        (adDynamic.Status == AdStatus.Published || adDynamic.Status == AdStatus.Approved))
                        continue;

                    if (!string.IsNullOrWhiteSpace(search) &&
                        !((string)adDynamic.Title).Contains(search, StringComparison.OrdinalIgnoreCase))
                        continue;

                    results.Add(ad);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process ad key: {Key}", key);
                }
            }

            _logger.LogInformation("Filtered down to {Count} ads after processing", results.Count);

            var paginated = results.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PaginatedAdResponseDto
            {
                Total = results.Count,
                Items = paginated
            };
        }

        public async Task<BulkAdActionResponse> BulkUpdateAdPublishStatusAsync(string subVertical,string userId,List<Guid> adIds,bool isPublished,CancellationToken cancellationToken = default)
        {
            try
            {
                var indexKey = subVertical.ToLowerInvariant() switch
                {
                    "items" => ItemsIndexKey,
                    "preloved" => PrelovedIndexKey,
                    "collectibles" => CollectiblesIndexKey,
                    "deals" => DealsIndexKey,
                    _ => throw new ArgumentException("Invalid sub-vertical.")
                };

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, indexKey) ?? new();
                var failedAds = new List<Guid>();
                var targetStatus = isPublished ? AdStatus.Published : AdStatus.Unpublished;

                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    if (!index.Contains(key))
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);
                    if (state.ValueKind != JsonValueKind.Object)
                    {
                        failedAds.Add(adId);
                        continue;
                    }

                    var storedUserId = state.TryGetProperty("userId", out var uid) ? uid.GetString() : string.Empty;
                    var currentStatus = state.TryGetProperty("status", out var st) && st.TryGetInt32(out var val)
                        ? (AdStatus)val
                        : AdStatus.Draft;

                    if (storedUserId != userId || currentStatus == targetStatus)
                    {
                        failedAds.Add(adId);
                    }
                }

                if (failedAds.Any())
                {
                    return new BulkAdActionResponse
                    {
                        SuccessCount = 0,
                        FailedAdIds = failedAds,
                        Message = "Some ads failed validation."
                    };
                }

                int successCount = 0;
                foreach (var adId in adIds)
                {
                    var key = $"ad-{adId}";
                    var state = await _dapr.GetStateAsync<JsonElement>(UnifiedStore, key, cancellationToken: cancellationToken);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(state.ToString()!)!;
                    dict["status"] = (int)targetStatus;
                    dict["createdAt"] = DateTime.UtcNow;

                    await _dapr.SaveStateAsync(UnifiedStore, key, dict, cancellationToken: cancellationToken);
                    successCount++;
                }

                return new BulkAdActionResponse
                {
                    SuccessCount = successCount,
                    FailedAdIds = new(),
                    Message = $"{successCount} ad(s) {(isPublished ? "published" : "unpublished")} successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk publish/unpublish failed.");
                throw new InvalidOperationException("An error occurred during bulk update.", ex);
            }
        }

        #region Private Methods
        private async Task IndexItemsToAzureSearch(ClassifiedsItems dto, CancellationToken cancellationToken)
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
        private async Task IndexPrelovedToAzureSearch(ClassifiedsPreloved dto, CancellationToken cancellationToken)
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
        private async Task IndexCollectiblesToAzureSearch(ClassifiedsCollectibles dto, CancellationToken cancellationToken)
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
        private async Task IndexDealsToAzureSearch(ClassifiedsDeals dto, CancellationToken cancellationToken)
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

               

        public async Task<string> FeatureClassifiedAd(ClassifiedsPromoteDto dto, string userId, CancellationToken cancellationToken)
        {
            try
            {
                object adItem = null;
                Console.WriteLine(userId);
                switch (dto.SubVertical)
                {
                    case SubVertical.Items:
                        adItem = await GetItemAdById(dto.AdId, cancellationToken);
                        break;
                    case SubVertical.Preloved:
                        adItem = await GetPrelovedAdById(dto.AdId, cancellationToken);
                        break;
                    case SubVertical.Collectibles:
                        adItem = await GetCollectiblesAdById(dto.AdId, cancellationToken);
                        break;
                    case SubVertical.Deals:
                        adItem = await GetDealsAdById(dto.AdId, cancellationToken);
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid SubVertical: {dto.SubVertical}");
                }
                if (adItem == null)
                {
                    _logger.LogError($"Ad with id {dto.AdId} not found in the {dto.SubVertical} vertical.");
                    throw new KeyNotFoundException($"Ad with id {dto.AdId} not found.");
                }
                if (adItem is ClassifiedsItems itemAd)
                {
                    if (itemAd.IsFeatured == true)
                    {
                        throw new ConflictException("This ad is already featured.");
                    }
                    itemAd.IsFeatured = true;
                    itemAd.UpdatedAt = DateTime.UtcNow;
                    itemAd.UserId = userId;
                    await _dapr.SaveStateAsync(UnifiedStore, $"ad-{itemAd.Id}", itemAd);
                }
                else if (adItem is ClassifiedsPreloved prelovedAd)
                {
                    if (prelovedAd.IsFeatured == true)
                    {
                        throw new ConflictException("This ad is already featured.");
                    }
                    prelovedAd.IsFeatured = true;
                    prelovedAd.UpdatedAt = DateTime.UtcNow;
                    prelovedAd.UserId = userId;
                    await _dapr.SaveStateAsync(UnifiedStore, $"ad-{prelovedAd.Id}", prelovedAd);
                }
                else if (adItem is ClassifiedsCollectibles collectiblesAd)
                {
                    if (collectiblesAd.IsFeatured == true)
                    {
                        throw new ConflictException("This ad is already featured.");
                    }
                    collectiblesAd.IsFeatured = true;
                    collectiblesAd.UpdatedAt = DateTime.UtcNow;
                    collectiblesAd.UserId = userId;
                    await _dapr.SaveStateAsync(UnifiedStore, $"ad-{collectiblesAd.Id}", collectiblesAd);
                }
                else if (adItem is ClassifiedsDeals dealsAd)
                {
                    if (dealsAd.IsFeatured == true)
                    {
                        throw new ConflictException("This ad is already featured.");
                    }
                    dealsAd.IsFeatured = true;
                    dealsAd.UpdatedAt = DateTime.UtcNow;
                    dealsAd.UserId = userId;
                    await _dapr.SaveStateAsync(UnifiedStore, $"ad-{dealsAd.Id}", dealsAd);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported ad type: {adItem.GetType().Name}");
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
        public async Task<string> PromoteClassifiedAd(ClassifiedsPromoteDto dto, string userId, CancellationToken cancellationToken)
        {
            try
            {
                object adItem = null;
                Console.WriteLine(userId);
                switch (dto.SubVertical)
                {
                    case SubVertical.Items:
                        adItem = await GetItemAdById(dto.AdId, cancellationToken);
                        break;
                    case SubVertical.Preloved:
                        adItem = await GetPrelovedAdById(dto.AdId, cancellationToken);
                        break;
                    case SubVertical.Collectibles:
                        adItem = await GetCollectiblesAdById(dto.AdId, cancellationToken);
                        break;
                    case SubVertical.Deals:
                        adItem = await GetDealsAdById(dto.AdId, cancellationToken);
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid SubVertical: {dto.SubVertical}");
                }
                if (adItem == null)
                {
                    _logger.LogError($"Ad with id {dto.AdId} not found in the {dto.SubVertical} vertical.");
                    throw new KeyNotFoundException($"Ad with id {dto.AdId} not found.");
                }
                if (adItem is ClassifiedsItems itemAd)
                {
                    if (itemAd.IsPromoted == true)
                    {
                        throw new ConflictException("This ad is already promoted.");
                    }
                    itemAd.IsPromoted = true;
                    itemAd.UpdatedAt = DateTime.UtcNow;
                    itemAd.UserId = userId;
                    await _dapr.SaveStateAsync(UnifiedStore, $"ad-{itemAd.Id}", itemAd);
                }
                else if (adItem is ClassifiedsPreloved prelovedAd)
                {
                    if (prelovedAd.IsPromoted == true)
                    {
                        throw new ConflictException("This ad is already promoted.");
                    }
                    prelovedAd.IsPromoted = true;
                    prelovedAd.UpdatedAt = DateTime.UtcNow;
                    prelovedAd.UserId = userId;
                    await _dapr.SaveStateAsync(UnifiedStore, $"ad-{prelovedAd.Id}", prelovedAd);
                }
                else if (adItem is ClassifiedsCollectibles collectiblesAd)
                {
                    if (collectiblesAd.IsPromoted == true)
                    {
                        throw new ConflictException("This ad is already promoted.");
                    }
                    collectiblesAd.IsPromoted = true;
                    collectiblesAd.UpdatedAt = DateTime.UtcNow;
                    collectiblesAd.UserId = userId;
                    await _dapr.SaveStateAsync(UnifiedStore, $"ad-{collectiblesAd.Id}", collectiblesAd);
                }
                else if (adItem is ClassifiedsDeals dealsAd)
                {
                    if (dealsAd.IsPromoted == true)
                    {
                        throw new ConflictException("This ad is already promoted.");
                    }
                    dealsAd.IsPromoted = true;
                    dealsAd.UpdatedAt = DateTime.UtcNow;
                    dealsAd.UserId = userId;
                    await _dapr.SaveStateAsync(UnifiedStore, $"ad-{dealsAd.Id}", dealsAd);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported ad type: {adItem.GetType().Name}");
                }
                return "The ad has been successfully marked as promoted.";
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


    }
}
