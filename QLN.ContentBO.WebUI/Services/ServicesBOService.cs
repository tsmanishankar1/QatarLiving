using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class ServicesBOService : ServiceBase<ServicesBOService>, IServiceBOService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ServicesBOService> _logger;

        public ServicesBOService(HttpClient httpClient, ILogger<ServicesBOService> logger)
            : base(httpClient, logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<HttpResponseMessage> GetServicesCategories()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/service/getallcategories");
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetServicesCategories");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> GetPaginatedP2PListing(
        string? sortBy = null,
        string? search = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        DateTime? publishedFrom = null,
        DateTime? publishedTo = null,
        int? status = null,
        bool? isPromoted = null,
        bool? isFeatured = null,
        int? pageNumber = null,
        int? pageSize = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(sortBy)) queryParams.Add($"sortBy={sortBy}");
                if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={search}");
                if (fromDate.HasValue) queryParams.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.ToString("o"))}");
                if (toDate.HasValue) queryParams.Add($"toDate={Uri.EscapeDataString(toDate.Value.ToString("o"))}");
                if (publishedFrom.HasValue) queryParams.Add($"publishedFrom={Uri.EscapeDataString(publishedFrom.Value.ToString("o"))}");
                if (publishedTo.HasValue) queryParams.Add($"publishedTo={Uri.EscapeDataString(publishedTo.Value.ToString("o"))}");
                if (status.HasValue) queryParams.Add($"status={status}");
                if (isPromoted.HasValue) queryParams.Add($"isPromoted={isPromoted.ToString().ToLower()}");
                if (isFeatured.HasValue) queryParams.Add($"isFeatured={isFeatured.ToString().ToLower()}");
                if (pageNumber.HasValue) queryParams.Add($"pageNumber={pageNumber}");
                if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize}");
                var query = string.Join("&", queryParams);
                var url = "api/servicebo/getallbo";
                if (queryParams.Count > 0)
                    url += "?" + query;
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetPaginatedP2PListing");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> GetPaginatedSubscriptionListing(
    string? sortBy = null,
    string? search = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    int? pageNumber = null,
    int? pageSize = null,
    string? subscriptionType = null)
        {
            try
            {
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(sortBy))
                    queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");

                if (!string.IsNullOrEmpty(search))
                    queryParams.Add($"search={Uri.EscapeDataString(search)}");

                if (fromDate.HasValue)
                    queryParams.Add($"startDate={Uri.EscapeDataString(fromDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"))}");

                if (toDate.HasValue)
                    queryParams.Add($"endDate={Uri.EscapeDataString(toDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"))}");

                if (pageNumber.HasValue)
                    queryParams.Add($"PageNumber={pageNumber}");

                if (pageSize.HasValue)
                    queryParams.Add($"PageSize={pageSize}");

                if (!string.IsNullOrEmpty(subscriptionType))
                    queryParams.Add($"subscriptionType={Uri.EscapeDataString(subscriptionType)}");

                var query = string.Join("&", queryParams);
                var url = "api/servicebo/getalladpayments";
                if (queryParams.Count > 0)
                    url += "?" + query;

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetPaginatedSubscriptionListing");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }


        public async Task<HttpResponseMessage> GetPaginatedP2PTransactionListing(
        string? sortBy = null,
        string? search = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? pageNumber = null,
        int? pageSize = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(sortBy)) queryParams.Add($"sortBy={sortBy}");
                if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={search}");
                if (fromDate.HasValue) queryParams.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.ToString("o"))}");
                if (toDate.HasValue) queryParams.Add($"toDate={Uri.EscapeDataString(toDate.Value.ToString("o"))}");
                if (pageNumber.HasValue) queryParams.Add($"pageNumber={pageNumber}");
                if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize}");
                var query = string.Join("&", queryParams);
                var url = "api/servicebo/getallp2pbo";
                if (queryParams.Count > 0)
                    url += "?" + query;
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetPaginatedP2PTransactionListing");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> GetPaginatedSubscriptionAdsListing(
        string? sortBy = null,
        string? search = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        DateTime? publishedFrom = null,
        DateTime? publishedTo = null,
        int? status = null,
        bool? isPromoted = null,
        bool? isFeatured = null,
        int? pageNumber = null,
        int? pageSize = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(sortBy)) queryParams.Add($"sortBy={sortBy}");
                if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={search}");
                if (fromDate.HasValue) queryParams.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.ToString("o"))}");
                if (toDate.HasValue) queryParams.Add($"toDate={Uri.EscapeDataString(toDate.Value.ToString("o"))}");
                if (publishedFrom.HasValue) queryParams.Add($"publishedFrom={Uri.EscapeDataString(publishedFrom.Value.ToString("o"))}");
                if (publishedTo.HasValue) queryParams.Add($"publishedTo={Uri.EscapeDataString(publishedTo.Value.ToString("o"))}");
                if (status.HasValue) queryParams.Add($"status={status}");
                if (isPromoted.HasValue) queryParams.Add($"isPromoted={isPromoted.ToString().ToLower()}");
                if (isFeatured.HasValue) queryParams.Add($"isFeatured={isFeatured.ToString().ToLower()}");
                if (pageNumber.HasValue) queryParams.Add($"pageNumber={pageNumber}");
                if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize}");
                var query = string.Join("&", queryParams);
                var url = "api/servicebo/getallsubscriptionadsbo";
                if (queryParams.Count > 0)
                    url += "?" + query;
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetPaginatedSubscriptionAdsListing");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> GetServiceById(Guid id)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/service/getbyid/{id}");
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetServiceById");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetAllZonesAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/v2/location/getAllZones");
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllZonesAsync Error: " + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> UpdateService(ServicesDto service)
        {
            try
            {
                var json = JsonSerializer.Serialize(service, new JsonSerializerOptions { WriteIndented = true });
                var request = new HttpRequestMessage(HttpMethod.Put, "api/service/update")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "UpdateService");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> ModerateBulkAction(object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                Logger.LogInformation("Sending payload to update featured event: {Payload}", json);

                var request = new HttpRequestMessage(HttpMethod.Post, "api/service/moderatebulk")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ModerateBulkAction");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> GetVerifiedSellerRequestAsync(int vertical)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/companyverified/profileStatusbyverified?vertical={vertical}");
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetProfileStatusByVerifiedAsync");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> UpdateServiceStatus(BulkModerationRequest requestModel)
        {
            try
            {
                var json = JsonSerializer.Serialize(requestModel, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine("the json is" + json);
                var request = new HttpRequestMessage(HttpMethod.Post, "api/service/moderatebulk")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "UpdateServiceStatus");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

    }
}
