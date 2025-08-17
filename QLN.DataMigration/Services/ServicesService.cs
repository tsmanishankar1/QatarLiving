using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IService;
using QLN.Common.Infrastructure.Model;

namespace QLN.DataMigration.Services
{
    public class ServicesService : IServices
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ServicesService> _logger;

        public ServicesService(DaprClient dapr, ILogger<ServicesService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }


        public Task<string> CreateCategory(CategoryDto dto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> CreateServiceAd(string uid, string userName, ServiceDto dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> DeleteServiceAdById(string userId, long id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Common.Infrastructure.Model.Services> FeatureService(FeatureServiceRequest request, string? uid, Guid subscriptionId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public async Task<List<CategoryDto>> GetAllCategories(string? vertical, string? subVertical, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new QueryString(string.Empty);

                if (!string.IsNullOrWhiteSpace(vertical))
                    query = query.Add("vertical", vertical);

                if (!string.IsNullOrWhiteSpace(subVertical))
                    query = query.Add("subVertical", subVertical);

                var uri = $"/api/service/getallcategories{query}";

                var response = await _dapr.InvokeMethodAsync<List<CategoryDto>>(
                    HttpMethod.Get,
                    ConstantValues.Services.ServiceAppId,
                    uri,
                    cancellationToken
                );

                return response ?? new List<CategoryDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving service categories");
                throw;
            }
        }

        public Task<ServicesPagedResponse<Common.Infrastructure.Model.Services>> GetAllServicesWithPagination(BasePaginationQuery? dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CategoryDto?> GetCategoryById(long id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Common.Infrastructure.Model.Services?> GetServiceAdById(long id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Common.Infrastructure.Model.Services?> GetServiceAdBySlug(string? slug, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SubscriptionBudgetDto> GetSubscriptionBudgetsAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SubscriptionBudgetDto> GetSubscriptionBudgetsAsyncBySubVertical(Guid subscriptionIdFromToken, int verticalId, int? subVerticalId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<Common.Infrastructure.Model.Services>> ModerateBulkService(BulkModerationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Common.Infrastructure.Model.Services> PromoteService(PromoteServiceRequest request, string? uid, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<Common.Infrastructure.Model.Services> PublishService(PublishServiceRequest request, string? uid, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<Common.Infrastructure.Model.Services> RefreshService(RefreshServiceRequest request, string? uid, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<string> UpdateCategory(CategoryDto dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> UpdateServiceAd(string userId, Common.Infrastructure.Model.Services dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
