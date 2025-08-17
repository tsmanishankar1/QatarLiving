using Dapr.Client;
using Dapr.Client.Autogen.Grpc.v1;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsFo;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Utilities;
using System.Net;
using System.Text;
using System.Text.Json;
namespace QLN.Backend.API.Service.ClassifiedsService
{
    public class ExternalClassifiedFoService:IClassifiedsFoService
    {
        private const string SERVICE_APP_ID = ConstantValues.ServiceAppIds.ClassifiedServiceApp;
        private const string Vertical = ConstantValues.ClassifiedsVertical;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEventlogger _log;
        private readonly DaprClient _dapr;
        public ExternalClassifiedFoService(DaprClient dapr, IEventlogger log, IHttpContextAccessor httpContextAccessor)
        {  
            _dapr = dapr;
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _httpContextAccessor = httpContextAccessor;  
        }
        public async Task<List<StoresDashboardHeaderDto>> GetStoresDashboardHeader(string? UserId, string? CompanyId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<StoresDashboardHeaderDto>>(
                   HttpMethod.Get,
                   SERVICE_APP_ID,
                   $"api/{Vertical}/stores-dashboard-headers?UserId={UserId}&CompanyId={CompanyId}",
                   cancellationToken
               );

                return result ?? new List<StoresDashboardHeaderDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to communicate with the internal service.", ex);
            }
        }

        public async Task<List<StoresDashboardSummaryDto>> GetStoresDashboardSummary(string? CompanyId, string? SubscriptionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<StoresDashboardSummaryDto>>(
                   HttpMethod.Get,
                   SERVICE_APP_ID,
                   $"api/{Vertical}/stores-dashboard-summarys?CompanyId={CompanyId}&SubscriptionId={SubscriptionId}",
                   cancellationToken
               );

                return result ?? new List<StoresDashboardSummaryDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to communicate with the internal service.", ex);
            }
        }
    }
}
