using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Backend.API.Service.V2ClassifiedBoService;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IClassifiedBoService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.ISearchService;
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

        public ExternalClassifiedStoresBOService(
            DaprClient dapr,
            ILogger<ExternalClassifiedStoresBOService> logger,
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

        //public async Task<string> CreateStoreSubscriptions(StoresSubscriptionDto dto, CancellationToken cancellationToken = default)
        //{
        //    try
        //    {
        //        var url = "api/v2/classifiedbo/stores-creates-subscriptions";
        //        var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, SERVICE_APP_ID, url);
        //        request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");



        //        var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
        //        if (response.StatusCode == HttpStatusCode.BadRequest)
        //        {
        //            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);

        //            string errorMessage;
        //            try
        //            {
        //                var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
        //                errorMessage = problem?.Detail ?? "Unknown error.";
        //            }
        //            catch
        //            {
        //                errorMessage = errorJson;
        //            }


        //            throw new InvalidDataException(errorMessage);
        //        }
        //        if (response.StatusCode == HttpStatusCode.Conflict)
        //        {
        //            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
        //            var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);

        //            throw new ConflictException(problem?.Detail ?? "Conflict error.");
        //        }
        //        response.EnsureSuccessStatusCode();

        //        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
        //        return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error creating company profile");
        //        throw;
        //    }
        //}
        //public async Task<string> EditStoreSubscriptions(int OrderID, string Status, CancellationToken cancellationToken = default)
        //{
        //    try
        //    {

        //        var queryParams = $"?OrderID={OrderID}&Status={Status}";
        //        var response = await _dapr.InvokeMethodAsync<string>(
        //            HttpMethod.Put,
        //            SERVICE_APP_ID,
        //            $"api/v2/classifiedbo/stores-edits-subscriptions{queryParams}",
        //            cancellationToken
        //        );

        //        return response;
        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error editing stores subscriptions.");
        //        throw;
        //    }
        //}

        public async Task<string> GetTestXMLValidation(CancellationToken cancellationToken = default)
        {
            try
            {

                var response = await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/stores-test-xml-validation",
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
        public async Task<string> GetProcessStoresXML(string Url, string? CompanyId, string? SubscriptionId, string UserName, CancellationToken cancellationToken = default)
        {
            try
            {

                var queryParams = $"?Url={Url}&CompanyId={CompanyId}&SubscriptionId={SubscriptionId}&UserName={UserName}";
                var response = await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/stores-processing-xml{queryParams}",
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

        public async Task<string> GetProcessStoresCSV(string Url, string CsvPlatform,string? CompanyId, string? SubscriptionId,
           string? UserId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                var queryParams = $"?Url={Url}&CsvPlatform={CsvPlatform}&CompanyId={CompanyId}&SubscriptionId={SubscriptionId}&UserId={UserId}";
                var response = await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/v2/classifiedbo/stores-processing-csv{queryParams}",
                    cts.Token
                );

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
