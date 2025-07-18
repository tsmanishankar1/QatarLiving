using Dapr;
using Dapr.Client;
using Dapr.Client.Autogen.Grpc.v1;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.IService.V2IClassifiedBoService;
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
    }
}
