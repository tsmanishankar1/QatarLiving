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
        int? pageNumber = null,
        int? pageSize = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(sortBy)) queryParams.Add($"sortBy={sortBy}");
                if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={search}");
                if (pageNumber.HasValue) queryParams.Add($"PageNumber={pageNumber}");
                if (pageSize.HasValue) queryParams.Add($"PageSize={pageSize}");

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
        
    }
}
