using Dapr.Client;
using Dapr.Client.Autogen.Grpc.v1;
using QLN.Backend.API.Service.ClassifiedBoService;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.DTO_s.ClassifiedsFo;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.IProductService;
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
        private readonly IV2SubscriptionService _subscriptionContext;
        private readonly ILogger<ExternalClassifiedFoService> _logger;
        public ExternalClassifiedFoService(DaprClient dapr, IEventlogger log, IHttpContextAccessor httpContextAccessor, IV2SubscriptionService subscriptionContext, ILogger<ExternalClassifiedFoService> logger)
        {  
            _dapr = dapr;
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _httpContextAccessor = httpContextAccessor; 
            _subscriptionContext = subscriptionContext;
            _logger = logger;
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

        public async Task<string> GetFOProcessStoresCSV(string Url, string CsvPlatform, string? CompanyId, string? SubscriptionId,
           string? UserId, string Domain, CancellationToken cancellationToken = default)
        {
            try
            {
                int ProductCount = 0;
                var allProducts = new List<StoreProducts>();
                var products = await GenericCSVReader.ReadCsv<ShopifyProduct>(Url);
                if (products != null)
                {
                    ProductCount = products.Count;
                    var canUse = await _subscriptionContext.ValidateSubscriptionUsageAsync(
                            Guid.Parse(SubscriptionId),
                            "publish",
                            ProductCount,
                            cancellationToken
                        );

                    if (!canUse)
                    {
                        _logger.LogWarning("Subscription {SubscriptionId} cannot perform pupblish. Insufficient quota.", Guid.Parse(SubscriptionId), "publish");
                        return "Insufficient quota";
                    }
                }
                else
                {
                    return "No products";
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                var queryParams = $"?Url={Url}&CsvPlatform={CsvPlatform}&CompanyId={CompanyId}&SubscriptionId={SubscriptionId}&UserId={UserId}&Domain={Domain}";
                var response = await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/stores-dashboard-processing-csv{queryParams}",
                    cts.Token
                );
               
                // No Need to record usage because stores needs to over write whenever new csv upload.

                //var reserved = await _subscriptionContext.RecordSubscriptionUsageAsync(
                //            Guid.Parse(SubscriptionId),
                //            "publish",
                //            ProductCount,
                //            cancellationToken
                //        );

                //if (!reserved)
                //{
                //    _logger.LogWarning("Failed to reserve quota for {SubscriptionId} and action {ActionName}.", Guid.Parse(SubscriptionId), "publish");
                //    return "Fail to reserve quota";
                //}


                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error in processing the csv file.");
                throw;
            }
        }
    }
}
