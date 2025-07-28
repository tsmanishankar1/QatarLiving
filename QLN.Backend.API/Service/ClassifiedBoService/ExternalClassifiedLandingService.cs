using Dapr;
using Dapr.Client;
using Dapr.Client.Autogen.Grpc.v1;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.DTO_s.ClassifiedsBoIndex;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.IService.V2IClassifiedBoService;
using QLN.Common.Infrastructure.Utilities;
using System.Globalization;
using System.Text;
using System.Text.Json;
using static QLN.Backend.API.Service.V2ClassifiedBoService.ExternalClassifiedLandingService;

namespace QLN.Backend.API.Service.V2ClassifiedBoService
{
    public class ExternalClassifiedLandingService : IClassifiedBoLandingService
    {

        private const string SERVICE_APP_ID = ConstantValues.ServiceAppIds.ClassifiedServiceApp;
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalClassifiedLandingService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileStorageBlobService _fileStorageBlob;
        private readonly ISearchService _searchService;

        public ExternalClassifiedLandingService(
            DaprClient dapr,
            ILogger<ExternalClassifiedLandingService> logger,
            IHttpContextAccessor httpContextAccessor,
            IFileStorageBlobService fileStorageBlob,
            ISearchService searchService)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor;
            _fileStorageBlob = fileStorageBlob;
            _searchService = searchService;
        }

        public async Task<string> CreateFeaturedCategory(string userId, string userName, FeaturedCategoryDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (string.IsNullOrWhiteSpace(dto.ImageUrl))
                throw new ArgumentException("Image is required.");

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.");

            var uploadedBlobKeys = new List<string>();

            try
            {
                var (imgExt, base64Data) = Base64ImageHelper.ParseBase64Image(dto.ImageUrl);
                var uniqueName = $"featured_{Guid.NewGuid():N}".Substring(0, 20) + $".{imgExt}";
                var imageUrl = await _fileStorageBlob.SaveBase64File(base64Data, uniqueName, "classifieds-images", cancellationToken);
                uploadedBlobKeys.Add(uniqueName);

                dto.ImageUrl = imageUrl;

                var response = await _dapr.InvokeMethodAsync<FeaturedCategoryDto, string>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/create-category?userid={userId}&username={userName}",
                    dto,
                    cancellationToken
                );

                return response ?? "Featured category created";
            }
            catch (Exception ex)
            {
                foreach (var key in uploadedBlobKeys)
                {
                    try
                    {
                        await _fileStorageBlob.DeleteFile(key, "classifieds-images", cancellationToken);
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx, "Failed to rollback uploaded blob file: {BlobName}", key);
                    }
                }

                _logger.LogError(ex, "Unexpected error in Create Featured Category.");
                throw new InvalidOperationException("Error creating featured category.", ex);
            }
        }

        public async Task<List<FeaturedCategory>> GetFeaturedCategoriesByVertical(string vertical, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));

            try
            {
                var trees = await _dapr.InvokeMethodAsync<List<FeaturedCategory>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"/api/v2/classifiedbo/getfeaturedcategoriesbyvertical/{vertical}",
                    cancellationToken);

                var l1s = trees?.ToList() ?? new List<FeaturedCategory>();

                return l1s;
            }
            catch (InvocationException ex)
            {
                _logger.LogError(ex, "Failed to get L1 categories for vertical: {Vertical}", vertical);
                throw new InvalidOperationException($"Failed to get L1 categories for {vertical}", ex);
            }
        }

        public async Task<string> ReorderFeaturedCategorySlots(string userId, LandingBoSlotReorderRequest dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<LandingBoSlotReorderRequest, string>(
                    HttpMethod.Put,
                    SERVICE_APP_ID,
                    $"/api/v2/classifiedbo/reorderfeaturedcategoryslots?userid={userId}",
                    dto,
                    cancellationToken);

                return response;
            }
            catch (InvocationException ex)
            {
                var errorJson = await ex.Response.Content.ReadAsStringAsync(cancellationToken);
                string errorMessage = errorJson;

                try
                {
                    var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                    errorMessage = problem?.Detail ?? ex.Message;
                }
                catch
                {
                    // Use raw message
                }

                _logger.LogError(ex, "Failed to reorder landing BO slots via Dapr.");
                throw new InvalidDataException(errorMessage, ex);
            }
        }

        public async Task<string> ReplaceFeaturedCategorySlots(string userId, LandingBoSlotReplaceRequest dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<LandingBoSlotReplaceRequest, string>(
                    HttpMethod.Put,
                    SERVICE_APP_ID,
                    $"/api/v2/classifiedbo/replace-slot?userid={userId}",
                    dto,
                    cancellationToken);

                return response;
            }
            catch (InvocationException ex)
            {
                var errorJson = await ex.Response.Content.ReadAsStringAsync(cancellationToken);
                string errorMessage = errorJson;

                try
                {
                    var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                    errorMessage = problem?.Detail ?? ex.Message;
                }
                catch
                {
                    // Use raw message
                }

                _logger.LogError(ex, "Failed to reorder landing BO slots via Dapr.");
                throw new InvalidDataException(errorMessage, ex);
            }
        }

        public async Task<string> DeleteFeaturedCategory(string categoryId, string userId, string vertical, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(categoryId))
                throw new ArgumentException("StoreId is required.", nameof(categoryId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.", nameof(userId));

            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));

            try
            {
                var queryParams = $"?categoryId={categoryId}&userId={userId}&vertical={vertical}";

                var response = await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Delete,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/deletefeaturedcategory{queryParams}",
                    cancellationToken
                );

                return response;
            }
            catch (InvocationException ex)
            {
                var errorJson = await ex.Response.Content.ReadAsStringAsync(cancellationToken);
                string errorMessage = errorJson;

                try
                {
                    var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                    errorMessage = problem?.Detail ?? ex.Message;
                }
                catch
                {
                    // Use raw message
                }

                _logger.LogError(ex, "Failed to delete featured category via Dapr.");
                throw new InvalidDataException(errorMessage, ex);
            }
        }

        public async Task<List<FeaturedCategory>> GetSlottedFeaturedCategory(string vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<List<FeaturedCategory>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"/api/v2/classifiedbo/getslottedfeaturedcategory?vertical={vertical}",
                    cancellationToken);

                return response ?? new List<FeaturedCategory>();
            }
            catch (InvocationException ex)
            {
                var errorJson = await ex.Response.Content.ReadAsStringAsync(cancellationToken);
                string errorMessage = errorJson;

                try
                {
                    var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                    errorMessage = problem?.Detail ?? ex.Message;
                }
                catch
                {
                    // Use raw message
                }

                _logger.LogError(ex, "Failed to get slotted featured category via Dapr.");
                throw new InvalidDataException(errorMessage, ex);
            }
        }

        public async Task<string> CreateSeasonalPick(string userId, string userName, SeasonalPicksDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (string.IsNullOrWhiteSpace(dto.ImageUrl))
                throw new ArgumentException("Image is required.");

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.");

            var uploadedBlobKeys = new List<string>();

            try
            {
                var (imgExt, base64Data) = Base64ImageHelper.ParseBase64Image(dto.ImageUrl);
                var uniqueName = $"featured_{Guid.NewGuid():N}".Substring(0, 20) + $".{imgExt}";
                var imageUrl = await _fileStorageBlob.SaveBase64File(base64Data, uniqueName, "classifieds-images", cancellationToken);
                uploadedBlobKeys.Add(uniqueName);

                dto.ImageUrl = imageUrl;

                var response = await _dapr.InvokeMethodAsync<SeasonalPicksDto, string>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/createseasonalpickbyid?userid={userId}&username={userName}",
                    dto,
                    cancellationToken
                );


                return response ?? "Seasonal pick created.";
            }
            catch (Exception ex)
            {
                foreach (var key in uploadedBlobKeys)
                {
                    try
                    {
                        await _fileStorageBlob.DeleteFile(key, "classifieds-images", cancellationToken);
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx, "Failed to rollback uploaded blob file: {BlobName}", key);
                    }
                }

                _logger.LogError(ex, "Unexpected error in CreateSeasonalPickAsync.");
                throw new InvalidOperationException("Error creating seasonal pick.", ex);
            }
        }

        public async Task<List<SeasonalPicks>> GetSeasonalPicks(string vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<List<SeasonalPicks>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/getseasonalpicks?vertical={vertical}",
                    cancellationToken
                );

                return response ?? new List<SeasonalPicks>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetSeasonalPicksAsync.");
                throw new InvalidOperationException("Error fetching seasonal picks.", ex);
            }
        }

        public async Task<List<SeasonalPicks>> GetSlottedSeasonalPicks(string vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<List<SeasonalPicks>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/seasonal-picks/slotted?vertical={vertical}",
                    cancellationToken
                );

                return response ?? new List<SeasonalPicks>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetSlottedSeasonalPicks.");
                throw new InvalidOperationException("Error fetching slotted seasonal picks.", ex);
            }
        }

        public async Task<string> ReplaceSlotWithSeasonalPick(string userId, ReplaceSeasonalPickSlotRequest dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.", nameof(userId));

            if (dto.TargetSlotId < 1 || dto.TargetSlotId > 6)
                throw new ArgumentOutOfRangeException(nameof(dto.TargetSlotId), "Slot number must be between 1 and 6.");

            try
            {

                var response = await _dapr.InvokeMethodAsync<ReplaceSeasonalPickSlotRequest, string>(
                    HttpMethod.Put,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/replace-seasonalpickslot?userid={userId}",
                    dto,
                    cancellationToken
                );

                return response ?? "Slot updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ReplaceSlotWithSeasonalPick (external call).");
                throw new InvalidOperationException("Error replacing slot with seasonal pick.", ex);
            }
        }

        public async Task<string> ReorderSeasonalPickSlots(string userId, SeasonalPickSlotReorderRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null.");

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required in the request payload.", nameof(userId));

            if (request.SlotAssignments == null || !request.SlotAssignments.Any())
                throw new ArgumentException("SlotAssignments list cannot be empty.", nameof(request.SlotAssignments));

            var invalidSlots = request.SlotAssignments
                .Where(x => x.SlotNumber < 1 || x.SlotNumber > 6)
                .ToList();

            if (invalidSlots.Any())
            {
                var invalidDetails = string.Join(", ", invalidSlots.Select(x => $"[PickId: {x.PickId}, Slot: {x.SlotNumber}]"));
                throw new ArgumentOutOfRangeException(nameof(request.SlotAssignments), $"SlotNumber must be between 1 and 6. Invalid entries: {invalidDetails}");
            }

            try
            {
                var queryParams = $"?userId={userId}";

                var response = await _dapr.InvokeMethodAsync<SeasonalPickSlotReorderRequest, string>(
                    HttpMethod.Put,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/reorder-seasonalpickslots{queryParams}",
                    request,
                    cancellationToken
                );

                return response ?? "Slot reordering completed successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ReorderSeasonalPickSlots (external call).");
                throw new InvalidOperationException("Error during slot reordering.", ex);
            }
        }

        public async Task<string> SoftDeleteSeasonalPick(string pickId, string userId, string vertical, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pickId))
                throw new ArgumentException("PickId is required.", nameof(pickId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.", nameof(userId));

            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));

            try
            {
                var queryParams = $"?pickId={pickId}&userId={userId}&vertical={vertical}";

                var response = await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Delete,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/softdelete-seasonalpick{queryParams}",
                    cancellationToken
                );

                return response ?? $"Soft delete triggered for pick ID '{pickId}'.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SoftDeleteSeasonalPick (external call). PickId: {PickId}, UserId: {UserId}", pickId, userId);
                throw new InvalidOperationException("Error while soft deleting seasonal pick.", ex);
            }
        }

        public async Task<string> CreateFeaturedStore(string userId, string userName, FeaturedStoreDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (string.IsNullOrWhiteSpace(dto.ImageUrl))
                throw new ArgumentException("Image is required.");

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.");

            var uploadedBlobKeys = new List<string>();

            try
            {
                var (imgExt, base64Data) = Base64ImageHelper.ParseBase64Image(dto.ImageUrl);
                var uniqueName = $"featured_{Guid.NewGuid():N}".Substring(0, 20) + $".{imgExt}";
                var imageUrl = await _fileStorageBlob.SaveBase64File(base64Data, uniqueName, "classifieds-images", cancellationToken);
                uploadedBlobKeys.Add(uniqueName);

                dto.ImageUrl = imageUrl;

                var response = await _dapr.InvokeMethodAsync<FeaturedStoreDto, string>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/create-featuredstorebyid?userid={userId}&username={userName}",
                    dto,
                    cancellationToken
                );

                return response ?? "Featured store created.";
            }
            catch (Exception ex)
            {
                foreach (var key in uploadedBlobKeys)
                {
                    try
                    {
                        await _fileStorageBlob.DeleteFile(key, "classifieds-images", cancellationToken);
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx, "Failed to rollback uploaded blob file: {BlobName}", key);
                    }
                }

                _logger.LogError(ex, "Unexpected error in CreateFeaturedStoreAsync.");
                throw new InvalidOperationException("Error creating featured store.", ex);
            }
        }

        public async Task<List<FeaturedStore>> GetFeaturedStores(string vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<List<FeaturedStore>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/getfeaturedstores?vertical={vertical}",
                    cancellationToken
                );

                return response ?? new List<FeaturedStore>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetFeaturedStoresAsync.");
                throw new InvalidOperationException("Error fetching featured stores.", ex);
            }
        }

        public async Task<List<FeaturedStore>> GetSlottedFeaturedStores(string vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<List<FeaturedStore>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/featured-stores/slotted?vertical={vertical}",
                    cancellationToken
                );

                return response ?? new List<FeaturedStore>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetSlottedFeaturedStores.");
                throw new InvalidOperationException("Error fetching slotted featured stores.", ex);
            }
        }

        public async Task<string> ReplaceSlotWithFeaturedStore(string userId, ReplaceFeaturedStoresSlotRequest dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.", nameof(userId));

            if (dto.TargetSlotId < 1 || dto.TargetSlotId > 6)
                throw new ArgumentOutOfRangeException(nameof(dto.TargetSlotId), "Slot number must be between 1 and 6.");

            try
            {

                var response = await _dapr.InvokeMethodAsync<ReplaceFeaturedStoresSlotRequest, string>(
                    HttpMethod.Put,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/replace-featuredstoreSlot?userid={userId}",
                    dto,
                    cancellationToken
                );

                return response ?? "Slot updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ReplaceSlotWithFeaturedStore (external call).");
                throw new InvalidOperationException("Error replacing slot with featured store.", ex);
            }
        }

        public async Task<string> ReorderFeaturedStoreSlots(string userId, FeaturedStoreSlotReorderRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null.");

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required in the request payload.", nameof(userId));

            if (request.SlotAssignments == null || !request.SlotAssignments.Any())
                throw new ArgumentException("SlotAssignments list cannot be empty.", nameof(request.SlotAssignments));

            var invalidSlots = request.SlotAssignments
                .Where(x => x.SlotNumber < 1 || x.SlotNumber > 6)
                .ToList();

            if (invalidSlots.Any())
            {
                var invalidDetails = string.Join(", ", invalidSlots.Select(x => $"[StoreId: {x.StoreId}, Slot: {x.SlotNumber}]"));
                throw new ArgumentOutOfRangeException(nameof(request.SlotAssignments), $"SlotNumber must be between 1 and 6. Invalid entries: {invalidDetails}");
            }

            try
            {
                var queryParams = $"?userId={userId}";

                var response = await _dapr.InvokeMethodAsync<FeaturedStoreSlotReorderRequest, string>(
                    HttpMethod.Put,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/reorder-featuredstoreslots{queryParams}",
                    request,
                    cancellationToken
                );

                return response ?? "Slot reordering completed successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ReorderFeaturedStoreSlots (external call).");
                throw new InvalidOperationException("Error during slot reordering.", ex);
            }
        }

        public async Task<string> SoftDeleteFeaturedStore(string storeId, string userId, string vertical, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storeId))
                throw new ArgumentException("StoreId is required.", nameof(storeId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.", nameof(userId));

            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));

            try
            {
                var queryParams = $"?storeId={storeId}&userId={userId}&vertical={vertical}";

                var response = await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Delete,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/softdeletefeaturedstore{queryParams}",
                    cancellationToken
                );

                return response ?? $"Soft delete triggered for featured store ID '{storeId}'.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SoftDeleteFeaturedStore (external call). StoreId: {StoreId}, UserId: {UserId}", storeId, userId);
                throw new InvalidOperationException("Error while soft deleting featured store.", ex);
            }
        }

        private async Task IndexServiceToAzureSearch(ClassifiedsItems dto, CancellationToken cancellationToken)
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
                Price = (double)dto.Price,
                PriceType = dto.PriceType,
                Location = dto.Location,
                Longitude = (double)dto.Longitude,
                Lattitude = (double)dto.Latitude,
                IsFeatured = dto.IsFeatured,
                IsPromoted = dto.IsPromoted,
                Status = dto.Status.ToString(),
                FeaturedExpiryDate = dto.FeaturedExpiryDate,
                PromotedExpiryDate = dto.PromotedExpiryDate,
                RefreshExpiryDate = dto.RefreshExpiryDate,
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
                    AdImageFileNames = i.AdImageFileNames,
                    Url = i.Url,
                    Order = i.Order
                }).ToList()
            };
            var indexRequest = new CommonIndexRequest
            {
                IndexName = ConstantValues.IndexNames.ClassifiedsItemsIndex,
                ClassifiedsItem = indexDoc
            };

            try
            {
                await _searchService.UploadAsync(indexRequest);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Payload: {Payload}", JsonSerializer.Serialize(indexRequest.ClassifiedsItem));
                throw;
            }
        }

        public async Task<List<ClassifiedsItems>> BulkAction(BulkActionRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "api/v2/classifiedbo/bulk-action-userid";
                var serviceRequest = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, SERVICE_APP_ID, url);
                serviceRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }
                    throw new InvalidDataException(errorMessage);
                }
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var moderatedAds = JsonSerializer.Deserialize<List<ClassifiedsItems>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (moderatedAds != null && moderatedAds.Any())
                {
                    foreach (var dto in moderatedAds)
                    {
                        await IndexServiceToAzureSearch(dto, cancellationToken);
                    }
                }
                return moderatedAds ?? new List<ClassifiedsItems>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moderating bulk services");
                throw;
            }

        }
        public async Task<TransactionListResponseDto> GetTransactionsAsync(
                    int pageNumber,
                    int pageSize,
                    string? searchText,
                    string? transactionType,
                    string? dateCreated,
                    string? datePublished,
                    string? dateStart,
                    string? dateEnd,
                    string? status,
                    string? paymentMethod,
                    string sortBy,
                    string sortOrder,
                    CancellationToken cancellationToken = default)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"pageNumber={pageNumber}",
                    $"pageSize={pageSize}"
                };

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    queryParams.Add($"searchText={Uri.EscapeDataString(searchText)}");
                }

                if (!string.IsNullOrWhiteSpace(transactionType))
                {
                    queryParams.Add($"transactionType={Uri.EscapeDataString(transactionType)}");
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    queryParams.Add($"status={Uri.EscapeDataString(status)}");
                }

                if (!string.IsNullOrWhiteSpace(paymentMethod))
                {
                    queryParams.Add($"paymentMethod={Uri.EscapeDataString(paymentMethod)}");
                }

                if (!string.IsNullOrWhiteSpace(dateCreated))
                {
                    queryParams.Add($"dateCreated={Uri.EscapeDataString(dateCreated)}");
                }

                if (!string.IsNullOrWhiteSpace(datePublished))
                {
                    queryParams.Add($"datePublished={Uri.EscapeDataString(datePublished)}");
                }

                if (!string.IsNullOrWhiteSpace(dateStart))
                {
                    queryParams.Add($"dateStart={Uri.EscapeDataString(dateStart)}");
                }

                if (!string.IsNullOrWhiteSpace(dateEnd))
                {
                    queryParams.Add($"dateEnd={Uri.EscapeDataString(dateEnd)}");
                }

                queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
                queryParams.Add($"sortOrder={Uri.EscapeDataString(sortOrder)}");

                var queryString = string.Join("&", queryParams);
                var endpoint = $"api/v2/classifiedbo/items/transactions?{queryString}";

                var response = await _dapr.InvokeMethodAsync<TransactionListResponseDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    endpoint,
                    cancellationToken
                );

                return response ?? new TransactionListResponseDto();
            }
            catch (Dapr.DaprException ex)
            {
                _logger.LogError(ex, "Failed to get transactions via Dapr");
                string errorMessage = ex.InnerException is HttpRequestException httpEx ? httpEx.Message : ex.Message;
                throw new InvalidOperationException(errorMessage, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting transactions");
                throw new InvalidOperationException("Error retrieving transactions", ex);
            }
        }
    }
}