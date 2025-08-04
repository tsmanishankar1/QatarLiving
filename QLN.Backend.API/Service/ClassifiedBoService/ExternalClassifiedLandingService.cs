using Azure.Storage.Blobs;
using Dapr;
using Dapr.Client;
using Dapr.Client.Autogen.Grpc.v1;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.DTO_s.ClassifiedsBoIndex;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.IService.V2IClassifiedBoService;
using QLN.Common.Infrastructure.Subscriptions;
using QLN.Common.Infrastructure.Utilities;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
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
            HttpStatusCode? failedStatusCode = null;
            string failedErrorMessage = null;

            try
            {

                var url = $"api/v2/classifiedbo/create-category?userid={userId}&username={userName}";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, SERVICE_APP_ID, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

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
                    failedStatusCode = response.StatusCode;
                    failedErrorMessage = errorMessage;
                    throw new InvalidDataException(errorMessage);
                }

                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    _logger.LogWarning("Received empty response from content service.");
                    return "Empty response from content service";
                }

                try
                {
                    return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize report response. Raw JSON: {RawJson}", rawJson);
                    return $"Unexpected response format: {rawJson}";
                }
            }
            catch (ConflictException ex)
            {
                throw;
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
                if (failedStatusCode == HttpStatusCode.Conflict)
                {
                    _logger.LogWarning(ex, "Conflict detected while creating report.");
                    throw new ConflictException(ex.Message);
                }
                _logger.LogError(ex, "Unexpected error in Create Featured Category.");
                throw new ConflictException(ex.Message);
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
            HttpStatusCode? failedStatusCode = null;
            string failedErrorMessage = null;
            try
            {
                var url = $"api/v2/classifiedbo/createseasonalpickbyid?userid={userId}&username={userName}";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, SERVICE_APP_ID, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

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
                    failedStatusCode = response.StatusCode;
                    failedErrorMessage = errorMessage;
                    throw new InvalidDataException(errorMessage);
                }

                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    _logger.LogWarning("Received empty response from content service.");
                    return "Empty response from content service";
                }

                try
                {
                    return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize report response. Raw JSON: {RawJson}", rawJson);
                    return $"Unexpected response format: {rawJson}";
                }
            }
            catch (ConflictException ex)
            {
                throw;
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
                if (failedStatusCode == HttpStatusCode.Conflict)
                {
                    _logger.LogWarning(ex, "Conflict detected while creating report.");
                    throw new ConflictException(ex.Message);
                }
                _logger.LogError(ex, "Unexpected error in CreateSeasonalPickAsync.");
                throw new ConflictException(ex.Message);
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
                .Where(x => x.SlotOrder < 1 || x.SlotOrder > 6)
                .ToList();

            if (invalidSlots.Any())
            {
                var invalidDetails = string.Join(", ", invalidSlots.Select(x => $"[PickId: {x.PickId}, Slot: {x.SlotOrder}]"));
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
            HttpStatusCode? failedStatusCode = null;
            string failedErrorMessage = null;
            try
            {

                var url = $"api/v2/classifiedbo/create-featuredstorebyid?userid={userId}&username={userName}";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, SERVICE_APP_ID, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

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
                    failedStatusCode = response.StatusCode;
                    failedErrorMessage = errorMessage;
                    throw new InvalidDataException(errorMessage);
                }

                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    _logger.LogWarning("Received empty response from content service.");
                    return "Empty response from content service";
                }

                try
                {
                    return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize report response. Raw JSON: {RawJson}", rawJson);
                    return $"Unexpected response format: {rawJson}";
                }
            }
            catch (ConflictException ex)
            {
                throw;
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
                if (failedStatusCode == HttpStatusCode.Conflict)
                {
                    _logger.LogWarning(ex, "Conflict detected while creating report.");
                    throw new ConflictException(ex.Message);
                }
                _logger.LogError(ex, "Unexpected error in CreateFeaturedStoreAsync.");
                throw new ConflictException(ex.Message);
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
                .Where(x => x.SlotOrder < 1 || x.SlotOrder > 6)
                .ToList();

            if (invalidSlots.Any())
            {
                var invalidDetails = string.Join(", ", invalidSlots.Select(x => $"[StoreId: {x.StoreId}, Slot: {x.SlotOrder}]"));
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

        public async Task<string> BulkItemsAction(BulkActionRequest request, string userId, CancellationToken cancellationToken = default)

        {

            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(userId))

                throw new ArgumentException("UserId is required.");

            var uploadedBlobKeys = new List<string>();

            HttpStatusCode? failedStatusCode = null;

            string failedErrorMessage = null;

            try

            {

                var url = "api/v2/classifiedbo/bulk-items-action-userid";

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

                    failedStatusCode = response.StatusCode;

                    failedErrorMessage = errorMessage;

                    throw new InvalidDataException(errorMessage);

                }

                response.EnsureSuccessStatusCode();

                return "Status Changed successfully";

            }

            catch (Exception ex)

            {

                if (failedStatusCode == HttpStatusCode.Conflict)

                {

                    _logger.LogWarning(ex, "Conflict detected while bulk items action.");

                    throw new ConflictException(ex.Message);

                }

                else if (failedStatusCode == HttpStatusCode.NotFound)

                {

                    throw new KeyNotFoundException(ex.Message);

                }

                _logger.LogError(ex, "Error bulk items action");

                throw;

            }

        }

        public async Task<string> BulkCollectiblesAction(BulkActionRequest request, string userId, CancellationToken cancellationToken = default)

        {

            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(userId))

                throw new ArgumentException("UserId is required.");

            var uploadedBlobKeys = new List<string>();

            HttpStatusCode? failedStatusCode = null;

            string failedErrorMessage = null;

            try

            {

                var url = "api/v2/classifiedbo/bulk-collectibles-action-userid";

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

                    failedStatusCode = response.StatusCode;

                    failedErrorMessage = errorMessage;

                    throw new InvalidDataException(errorMessage);

                }

                response.EnsureSuccessStatusCode();

                return "Action Processed successfully";

            }

            catch (Exception ex)

            {

                if (failedStatusCode == HttpStatusCode.Conflict)

                {

                    _logger.LogWarning(ex, "Conflict detected while bulk collectibles action.");

                    throw new ConflictException(ex.Message);

                }

                else if (failedStatusCode == HttpStatusCode.NotFound)

                {

                    throw new KeyNotFoundException(ex.Message);

                }

                _logger.LogError(ex, "Error bulk collectibles action");

                throw;

            }

        }

        public async Task<TransactionListResponseDto> GetTransactionsAsync(
            TransactionFilterRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoint = $"api/v2/classifiedbo/items/transactions";

                var response = await _dapr.InvokeMethodAsync<TransactionFilterRequestDto, TransactionListResponseDto>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    endpoint,
                    request,
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


        public async Task<PaginatedResult<PrelovedAdPaymentSummaryDto>> GetAllPrelovedAdPaymentSummaries(
     int? pageNumber = 1,
     int? pageSize = 12,
     string? search = null,
     string? sortBy = null,
     CancellationToken cancellationToken = default)
        {
            var queryParams = new List<string>
            {
                $"pageNumber={pageNumber ?? 1}",
                $"pageSize={pageSize ?? 12}"
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                queryParams.Add($"search={Uri.EscapeDataString(search)}");
            }

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
            }

            var url = $"/api/v2/classifiedbo/preloved-ads/payment-summary?{string.Join("&", queryParams)}";

            try
            {                
                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    url);

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var error = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                    throw new InvalidOperationException(error?.Detail ?? errorJson);
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                
                var result = JsonSerializer.Deserialize<PaginatedResult<PrelovedAdPaymentSummaryDto>>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result == null)
                {
                    throw new InvalidOperationException("Failed to deserialize response content to PaginatedResult.");
                }

                return result;
            }
            catch (JsonException jsonEx)
            {
                throw new InvalidOperationException("Failed to parse response JSON.", jsonEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching preloved ad payment summaries.");
                throw new InvalidOperationException("Error fetching preloved ad payment summaries.", ex);
            }
        }

        public async Task<PaginatedResult<PrelovedAdSummaryDto>> GetAllPrelovedBoAds(
            string? sortBy = "CreationDate",
            string? search = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            DateTime? publishedFrom = null,
            DateTime? publishedTo = null,
            int? status = null,
            bool? isFeatured = null,
            bool? isPromoted = null,
            int pageNumber = 1,
            int pageSize = 12,
            CancellationToken cancellationToken = default)
        {
            var queryParams = new List<string>
            {
                $"pageNumber={pageNumber}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(sortBy))
                queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");

            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");

            if (fromDate.HasValue)
                queryParams.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.ToString("o"))}");

            if (toDate.HasValue)
                queryParams.Add($"toDate={Uri.EscapeDataString(toDate.Value.ToString("o"))}");

            if (publishedFrom.HasValue)
                queryParams.Add($"publishedFrom={Uri.EscapeDataString(publishedFrom.Value.ToString("o"))}");

            if (publishedTo.HasValue)
                queryParams.Add($"publishedTo={Uri.EscapeDataString(publishedTo.Value.ToString("o"))}");

            if (status.HasValue)
                queryParams.Add($"status={status.Value}");

            if (isFeatured.HasValue)
                queryParams.Add($"isFeatured={isFeatured.Value.ToString().ToLowerInvariant()}");

            if (isPromoted.HasValue)
                queryParams.Add($"isPromoted={isPromoted.Value.ToString().ToLowerInvariant()}");

            var url = $"/api/v2/classifiedbo/getallprelovedads?{string.Join("&", queryParams)}";

            try
            {
                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    url);

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var error = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                    throw new InvalidOperationException(error?.Detail ?? errorJson);
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                var result = JsonSerializer.Deserialize<PaginatedResult<PrelovedAdSummaryDto>>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result == null)
                {
                    throw new InvalidOperationException("Failed to deserialize response content to PaginatedResult.");
                }

                return result;
            }
            catch (JsonException jsonEx)
            {
                throw new InvalidOperationException("Failed to parse response JSON.", jsonEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching preloved ad summaries.");
                throw new InvalidOperationException("Error fetching preloved ad summaries.", ex);
            }
        }

        public async Task<PaginatedResult<DealsAdSummaryDto>> GetAllDeals(
            int? pageNumber = 1,
            int? pageSize = 12,
            string? search = null,
            string? sortBy = null,
            CancellationToken cancellationToken = default)
        {
            var queryParams = new List<string>
            {
                $"pageNumber={pageNumber ?? 1}",
                $"pageSize={pageSize ?? 12}"
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                queryParams.Add($"search={Uri.EscapeDataString(search)}");
            }

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
            }

            var url = $"/api/v2/classifiedbo/getdealsSummary?{string.Join("&", queryParams)}";


            try
            {
                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    url);

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var error = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                    throw new InvalidOperationException(error?.Detail ?? errorJson);
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                var result = JsonSerializer.Deserialize<PaginatedResult<DealsAdSummaryDto>>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result == null)
                {
                    throw new InvalidOperationException("Failed to deserialize response content to PaginatedResult.");
                }

                return result;
            }
            catch (JsonException jsonEx)
            {
                throw new InvalidOperationException("Failed to parse response JSON.", jsonEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching deals ad payment summaries.");
                throw new InvalidOperationException("Error fetching deals ad payment summaries.", ex);
            }
        }


        public async Task<PaginatedResult<DealsViewSummaryDto>> DealsViewSummary(int? pageNumber = 1, int? pageSize = 12,
            string? search = null,
            string? sortBy = null,
            string? status = null,
            bool? isPromoted = null,
            bool? isFeatured = null,
            CancellationToken cancellationToken = default)
        {
            var queryParams = new List<string>
            {
                $"pageNumber={pageNumber ?? 1}",
                $"pageSize={pageSize ?? 12}"
            };

            if (!string.IsNullOrWhiteSpace(search))

                queryParams.Add($"search={Uri.EscapeDataString(search)}");

            if (!string.IsNullOrWhiteSpace(sortBy))

                queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");

            if (!string.IsNullOrWhiteSpace(status))

                queryParams.Add($"status={Uri.EscapeDataString(status)}");

            if (isPromoted.HasValue)

                queryParams.Add($"isPromoted={isPromoted.Value.ToString().ToLower()}");

            if (isFeatured.HasValue)

                queryParams.Add($"isFeatured={isFeatured.Value.ToString().ToLower()}");

            var url = $"/api/v2/classifiedbo/DealsViewSummary?{string.Join("&", queryParams)}";

            try
            {
                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    url);

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var error = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                    throw new InvalidOperationException(error?.Detail ?? errorJson);
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                var result = JsonSerializer.Deserialize<PaginatedResult<DealsViewSummaryDto>>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result == null)
                    throw new InvalidOperationException("Failed to deserialize response content to PaginatedResult.");
                return result;
            }
            catch (JsonException jsonEx)
            {
                throw new InvalidOperationException("Failed to parse response JSON.", jsonEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching deals ad payment summaries.");
                throw new InvalidOperationException("Error fetching deals ad payment summaries.", ex);
            }

        }

        public async Task<string> SoftDeleteDeals(DealsBulkDelete dto, string userId, CancellationToken cancellationToken = default)
        {
            if (dto.AdId == null || dto.AdId.Count == 0)
                throw new ArgumentException("At least one Ad ID is required.", nameof(dto.AdId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.", nameof(userId));

            var queryParam = $"?userId={Uri.EscapeDataString(userId)}";

            try
            {
                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Delete,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/softdeletedeals{queryParam}"
                );

                var payload = new { adid = dto.AdId };

                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync(cancellationToken);
                return result ?? $"Soft delete triggered for {dto.AdId.Count} deal(s).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SoftDeleteDeals (bulk external call).");
                throw new InvalidOperationException("Error while performing bulk soft delete.", ex);
            }
        }
        public async Task<string> BulkPrelovedAction(BulkActionRequest request, string userId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.");

            var uploadedBlobKeys = new List<string>();
            HttpStatusCode? failedStatusCode = null;
            string failedErrorMessage = null;
            try
            {
                var url = "api/v2/classifiedbo/bulk-preloved-action-userid";
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
                    failedStatusCode = response.StatusCode;
                    failedErrorMessage = errorMessage;
                    throw new InvalidDataException(errorMessage);
                }
                response.EnsureSuccessStatusCode();
                return "Action Processed successfully";
            }
            catch (Exception ex)
            {
                if (failedStatusCode == HttpStatusCode.Conflict)
                {
                    _logger.LogWarning(ex, "Conflict detected while bulk preloved action.");
                    throw new ConflictException(ex.Message);
                }
                else if (failedStatusCode == HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException(ex.Message);
                }
                _logger.LogError(ex, "Error bulk preloved action");
                throw;
            }
        }

        public async Task<PrelovedTransactionListResponseDto> GetPrelovedTransactionsAsync(int pageNumber,
            int pageSize,
            string? searchText,
            string? dateCreated,
            string? datePublished,
            string? dateStart,
            string? dateEnd,
            string? status,
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

                if (!string.IsNullOrWhiteSpace(status))
                {
                    queryParams.Add($"status={Uri.EscapeDataString(status)}");
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
                var endpoint = $"api/v2/classifiedbo/preloved/transactions?{queryString}";

                var response = await _dapr.InvokeMethodAsync<PrelovedTransactionListResponseDto>(
               HttpMethod.Get,
               SERVICE_APP_ID,
               endpoint,
               cancellationToken
               );

                return response ?? new PrelovedTransactionListResponseDto();
            }
            catch (Dapr.DaprException ex)
            {
                _logger.LogError(ex, "Failed to get transactions via Dapr");
                string errorMessage = ex.InnerException is HttpRequestException httpEx ? httpEx.Message : ex.Message;
                throw new InvalidOperationException(errorMessage, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moderating bulk services");
                throw;
            }

        }

        public async Task<List<StoresSubscriptionDto>> getStoreSubscriptions(string? subscriptionType, string? filterDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var queryParams = $"?subscriptionType={subscriptionType}&filterDate={filterDate}";
                var response = await _dapr.InvokeMethodAsync<List<StoresSubscriptionDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                   
                    $"api/v2/classifiedbo/getstoresubscriptions{queryParams}",
                    cancellationToken
                );

                return response ?? new List<StoresSubscriptionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in stores subscriptions.");
                throw new InvalidOperationException("Error fetching stores subscriptions.", ex);
            }
        }
        public async Task<string> CreateStoreSubscriptions(StoresSubscriptionDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "api/v2/classifiedbo/create-store-subscriptions";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, SERVICE_APP_ID, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");



                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);

                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }

                    
                    throw new InvalidDataException(errorMessage);
                }
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                   
                    throw new ConflictException(problem?.Detail ?? "Conflict error.");
                }
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                
                _logger.LogError(ex, "Error creating company profile");
                throw;
            }
        }
        public async Task<string> EditStoreSubscriptions(int OrderID, string Status, CancellationToken cancellationToken = default)
        {
            try
            {
  
                var queryParams = $"?OrderID={OrderID}&Status={Status}";
                var response = await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Put,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/edit-store-subscriptions{queryParams}",
                    cancellationToken
                );

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error editing stores subscriptions.");
                throw;
            }
        }
        public async Task<ClassifiedsBoItemsResponseDto> GetAllItems(GetAllSearch request, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "api/v2/classifiedbo/getall-items";

                var response = await _dapr.InvokeMethodAsync<GetAllSearch, ClassifiedsBoItemsResponseDto>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    url,
                    request,
                    cancellationToken
                );

                return response ?? new ClassifiedsBoItemsResponseDto();
            }
            catch (DaprException daprEx)
            {
                _logger.LogError(daprEx, "Dapr error occurred while calling BulkItems. StatusCode: {StatusCode}", daprEx.Data["StatusCode"]);

                if (daprEx.Data.Contains("StatusCode"))
                {
                    var statusCode = daprEx.Data["StatusCode"]?.ToString();
                    if (statusCode == "409")
                    {
                        throw new ConflictException(daprEx.Message);
                    }
                    else if (statusCode == "404")
                    {
                        throw new KeyNotFoundException(daprEx.Message);
                    }
                }

                throw new Exception($"Service communication error: {daprEx.Message}", daprEx);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error occurred while calling BulkItems");
                throw new Exception($"Network error: {httpEx.Message}", httpEx);
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
            {
                _logger.LogError(tcEx, "Timeout occurred while calling BulkItems");
                throw new Exception("Request timed out", tcEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while calling BulkItems");
                throw new Exception($"Unexpected error: {ex.Message}", ex);
            }
        }

        public async Task<ClassifiedsBoCollectiblesResponseDto> GetAllCollectibles(GetAllSearch request, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "api/v2/classifiedbo/getall-collectibles";

                var response = await _dapr.InvokeMethodAsync<GetAllSearch, ClassifiedsBoCollectiblesResponseDto>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    url,
                    request,
                    cancellationToken
                );

                return response ?? new ClassifiedsBoCollectiblesResponseDto();
            }
            catch (DaprException daprEx)
            {
                _logger.LogError(daprEx, "Dapr error occurred while calling BulkItems. StatusCode: {StatusCode}", daprEx.Data["StatusCode"]);

                if (daprEx.Data.Contains("StatusCode"))
                {
                    var statusCode = daprEx.Data["StatusCode"]?.ToString();
                    if (statusCode == "409")
                    {
                        throw new ConflictException(daprEx.Message);
                    }
                    else if (statusCode == "404")
                    {
                        throw new KeyNotFoundException(daprEx.Message);
                    }
                }

                throw new Exception($"Service communication error: {daprEx.Message}", daprEx);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error occurred while calling BulkItems");
                throw new Exception($"Network error: {httpEx.Message}", httpEx);
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
            {
                _logger.LogError(tcEx, "Timeout occurred while calling BulkItems");
                throw new Exception("Request timed out", tcEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while calling BulkItems");
                throw new Exception($"Unexpected error: {ex.Message}", ex);
            }
        }   

       

    }

        public async Task<List<SubscriptionTypes>> GetSubscriptionTypes(CancellationToken cancellationToken = default)
        {
            try
            {
               
                var response = await _dapr.InvokeMethodAsync<List<SubscriptionTypes>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/get-subscription-types",
                    cancellationToken
                );

                return response ?? new List<SubscriptionTypes>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in listing subscription types.");
                throw new InvalidOperationException("Error fetching in listing subscription types.", ex);
            }
        }
        public async Task<SubscriptionTypes> GetSubscriptionById(int Id,CancellationToken cancellationToken = default)
        {
            try
            {
                var queryParams = $"?SubscriptionId={Id}";
                var response = await _dapr.InvokeMethodAsync<SubscriptionTypes>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/get-subscription-id{queryParams}",
                    cancellationToken
                );

                return response ?? new SubscriptionTypes();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in selecting subscription types.");
                throw new InvalidOperationException("Error in selecting subscription types.", ex);
            }
        }

        public async Task<string> GetTestXMLValidation(CancellationToken cancellationToken = default)
        {
            try
            {

                var response = await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/get-test-xml-validation",
                    cancellationToken
                );

                return response ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in testing validation xml");
                throw new InvalidOperationException("Error in testing validation xml", ex);
            }
        }

       
        public async Task<string> GetProcessStoresXML(string Url, string CompanyId, int SubscriptionId, string UserName, CancellationToken cancellationToken = default)
        {
            try
            {

                var queryParams = $"?Url={Url}&CompanyId={CompanyId}&SubscriptionId={SubscriptionId}&UserName={UserName}";
                var response = await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/get-process-store-xml{queryParams}",
                    cancellationToken
                );

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error in processing the xml file.");
                throw;
            }
        }

    }
}
