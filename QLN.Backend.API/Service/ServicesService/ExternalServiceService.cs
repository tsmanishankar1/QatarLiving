using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService;

namespace QLN.Backend.API.Service.ServicesService
{
    public class ExternalServiceService : IServicesService
    {
        private const string SERVICE_APP_ID = "qln-classified-ms";
        private readonly DaprClient _dapr;
        private readonly IEventlogger _log;

        public ExternalServiceService(DaprClient dapr, IEventlogger log)
        {
            _dapr = dapr;
            _log = log;
        }

        public async Task<ServiceDashboardWithAdsDto> GetDashboardAndAds(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<ServiceDashboardWithAdsDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/services/dashboard-with-ads/byId?userId={userId}",
                    cancellationToken
                );
                return result;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }
    }
}
