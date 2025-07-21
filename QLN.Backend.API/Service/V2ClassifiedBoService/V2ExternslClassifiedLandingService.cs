using Dapr;
using Dapr.Client;
using Dapr.Client.Autogen.Grpc.v1;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.IService.V2IClassifiedBoService;
using QLN.Common.Infrastructure.Utilities;
using System.Text;
using System.Text.Json;
using static QLN.Backend.API.Service.V2ClassifiedBoService.V2ExternslClassifiedLandingService;

namespace QLN.Backend.API.Service.V2ClassifiedBoService
{
    public class V2ExternslClassifiedLandingService : V2IClassifiedBoLandingService
    {

        private const string SERVICE_APP_ID = ConstantValues.ServiceAppIds.ClassifiedServiceApp;
        private readonly DaprClient _dapr;
        private readonly ILogger<V2ExternslClassifiedLandingService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileStorageBlobService _fileStorageBlob;
        private readonly ISearchService _searchService;

        public V2ExternslClassifiedLandingService(
            DaprClient dapr,
            ILogger<V2ExternslClassifiedLandingService> logger,
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



        public async Task<List<L1CategoryDto>> GetL1CategoriesByVerticalAsync(string vertical, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));

            try
            {
                var trees = await _dapr.InvokeMethodAsync<List<CategoryTreeDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"/api/v2/classifiedbo/lookup/l1-categories/{vertical}",
                    cancellationToken);

                var l1s = trees?
                    .Select(t => new L1CategoryDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Fields = t.Fields,
                        Vertical = vertical
                    })
                    .ToList() ?? new List<L1CategoryDto>();

                return l1s;
            }
            catch (InvocationException ex)
            {
                _logger.LogError(ex, "Failed to get L1 categories for vertical: {Vertical}", vertical);
                throw new InvalidOperationException($"Failed to get L1 categories for {vertical}", ex);
            }
        }


        public async Task<string> CreateLandingBoItemAsync(string userId,V2ClassifiedLandingBoDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto == null) throw new ArgumentNullException(nameof(dto));

                var response = await _dapr.InvokeMethodAsync<V2ClassifiedLandingBoDto, string>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/v2/classifiedbo/classified-landing/slotbyid",
                    dto,
                    cancellationToken
                );

                return response;
            }
            catch (InvocationException ex)
            {
                _logger.LogError(ex, "Dapr invocation failed while creating featured category.");
                throw new InvalidOperationException("Failed to create featured category from external service.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreateFeaturedCategoryAsync.");
                throw;
            }
        }

        public async Task<string> CreateSeasonalPick(SeasonalPicksDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (string.IsNullOrWhiteSpace(dto.ImageUrl))
                throw new ArgumentException("Image is required.");

            if (string.IsNullOrWhiteSpace(dto.UserId))
                throw new ArgumentException("UserId is required.");

            var uploadedBlobKeys = new List<string>();

            try
            {
                var (imgExt, base64Data) = Base64ImageHelper.ParseBase64Image(dto.ImageUrl);
                var uniqueName = $"seasonal_{dto.UserId}_{Guid.NewGuid():N}".Substring(0, 10) + $".{imgExt}";
                var imageUrl = await _fileStorageBlob.SaveBase64File(base64Data, uniqueName, "classifieds-images", cancellationToken);
                uploadedBlobKeys.Add(uniqueName);

                dto.ImageUrl = imageUrl;

                var response = await _dapr.InvokeMethodAsync<SeasonalPicksDto, string>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/v2/classifiedbo/createSeasonalPickById",  
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

        public async Task<List<SeasonalPicksDto>> GetSeasonalPicks(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<List<SeasonalPicksDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/v2/classifiedbo/getSeasonalPicks",
                    cancellationToken
                );

                return response ?? new List<SeasonalPicksDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetSeasonalPicksAsync.");
                throw new InvalidOperationException("Error fetching seasonal picks.", ex);
            }
        }

        public async Task<List<SeasonalPicksDto>> GetSlottedSeasonalPicks(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<List<SeasonalPicksDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/v2/classifiedbo/seasonal-picks/slotted",
                    cancellationToken
                );

                return response ?? new List<SeasonalPicksDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetSlottedSeasonalPicks.");
                throw new InvalidOperationException("Error fetching slotted seasonal picks.", ex);
            }
        }

        public async Task<string> ReplaceSlotWithSeasonalPick(string userId, Guid pickId, int slot, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.", nameof(userId));

            if (slot < 1 || slot > 6)
                throw new ArgumentOutOfRangeException(nameof(slot), "Slot number must be between 1 and 6.");

            try
            {
                var queryParams = $"?pickId={pickId}&slot={slot}&userId={userId}";

                var response = await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Put,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/replaceSeasonalPickSlot{queryParams}",
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

        public async Task<string> ReorderSeasonalPickSlots(SeasonalPickSlotReorderRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null.");

            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new ArgumentException("UserId is required in the request payload.", nameof(request.UserId));

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
                var queryParams = $"?userId={request.UserId}";

                var response = await _dapr.InvokeMethodAsync<SeasonalPickSlotReorderRequest, string>(
                    HttpMethod.Put,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/reorderSeasonalPickSlots{queryParams}",
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

        public async Task<string> SoftDeleteSeasonalPick(string pickId, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pickId))
                throw new ArgumentException("PickId is required.", nameof(pickId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.", nameof(userId));

            try
            {
                var queryParams = $"?pickId={pickId}&userId={userId}";

                var response = await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Delete,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/softDeleteSeasonalPick{queryParams}",
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

    }
}
