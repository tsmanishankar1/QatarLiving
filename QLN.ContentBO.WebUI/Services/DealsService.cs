using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class DealsService : ServiceBase<DealsService>, IDealsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        public DealsService(HttpClient httpClient, ILogger<DealsService> Logger)
            : base(httpClient, Logger)
        {
            _httpClient = httpClient;
            _logger = Logger;
        }

        public async Task<HttpResponseMessage?> BulkActionAsync(List<long?> adIds, int action, string? reason = null, string? comments = null)
        {
            try
            {
                var payload = new
                {
                    AdIds = adIds,
                    Action = action,
                    Reason = reason,
                    Comments = comments
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var requestUrl = "/api/v2/classifiedbo/bulk-deals-action";
                return await _httpClient.PostAsync(requestUrl, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BulkActionAsync");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
