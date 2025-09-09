using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class ReportService : ServiceBase<ReportService>, IReportService
    {
        private readonly HttpClient _httpClient;

        public ReportService(HttpClient httpClient, ILogger<ReportService> Logger)
            : base(httpClient, Logger)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> UpdateReport(string endpoint, string id, bool isKeep, bool isDelete)
        {
            try
            {
                var payload = new
                {
                    reportId = id,
                    isKeep,
                    isDelete
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Put, endpoint)
                {
                    Content = content
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in UpdateReport");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> DeleteCommunitylPosts(string pickId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete,
                    $"/api/v2/community/deletePost/{pickId}");
                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Logger.LogError("DeleteCommunitylPosts " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> GetReportsWithPaginationAsync(
            string endpoint,
            string? sortOrder = "desc",
            int pageNumber = 1,
            int pageSize = 12,
            string? searchTerm = null
        )
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"sortOrder={sortOrder}",
                    $"pageNumber={pageNumber}",
                    $"pageSize={pageSize}"
                };

                if (!string.IsNullOrWhiteSpace(searchTerm))
                    queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");

                var queryString = "?" + string.Join("&", queryParams);
                var request = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}{queryString}");

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in GetReportsWithPaginationAsync");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

    }
}
