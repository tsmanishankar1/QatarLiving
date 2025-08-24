using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Backend.API.Service.V2ClassifiedBoService;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IClassifiedBoService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.Utilities;
using System.Net;
using System.Text;
using System.Text.Json;
namespace QLN.Backend.API.Service.ClassifiedBoService
{
    public class ExternalClassifiedStoresBOService: IClassifiedStoresBOService
    {
        private const string SERVICE_APP_ID = ConstantValues.ServiceAppIds.ClassifiedServiceApp;
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalClassifiedStoresBOService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileStorageBlobService _fileStorageBlob;
        private readonly ISearchService _searchService;
        private readonly IV2SubscriptionService _subscriptionContext;
        public ExternalClassifiedStoresBOService(
            DaprClient dapr,
            ILogger<ExternalClassifiedStoresBOService> logger,
            IHttpContextAccessor httpContextAccessor,
            IFileStorageBlobService fileStorageBlob,
            ISearchService searchService, IV2SubscriptionService subscriptionContext)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor;
            _fileStorageBlob = fileStorageBlob;
            _searchService = searchService;
            _subscriptionContext = subscriptionContext;
        }

        public async Task<ClassifiedBOPageResponse<ViewStoresSubscriptionDto>> getStoreSubscriptions(string? subscriptionType, string? filterDate, int? Page, int? PageSize, string? Search, CancellationToken cancellationToken = default)
        {
            try
            {
                var queryParams = $"?subscriptionType={subscriptionType}&filterDate={filterDate}&Page={Page}&PageSize={PageSize}&Search={Search}";
                var response = await _dapr.InvokeMethodAsync<ClassifiedBOPageResponse<ViewStoresSubscriptionDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,

                    $"api/v2/classifiedbo/stores-get-subscriptions{queryParams}",
                    cancellationToken
                );

                return response ?? new ClassifiedBOPageResponse<ViewStoresSubscriptionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in stores subscriptions.");
                throw new InvalidOperationException("Error fetching stores subscriptions.", ex);
            }
        }

        public async Task<string> GetProcessStoresCSV(string Url, string CsvPlatform,string? CompanyId, string? SubscriptionId,
           string? UserId,string Domain, CancellationToken cancellationToken = default)
        {
            try
            {
                int ProductCount = 0;
                var allProducts = new List<StoreProducts>();
                var products = await GenericCSVReader.ReadCsv<ShopifyProduct>(Url);
                if (products != null) 
                {
                    ProductCount=products.Count;
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
                    $"api/v2/classifiedbo/stores-processing-csv{queryParams}",
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
