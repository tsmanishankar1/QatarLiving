using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;

namespace QLN.ContentBO.WebUI.Services
{
    public class SubscriptionService : ServiceBase<SubscriptionService>, ISubscriptionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        public SubscriptionService(HttpClient httpClient, ILogger<SubscriptionService> Logger)
            : base(httpClient, Logger)
        {
            _httpClient = httpClient;
            _logger = Logger;
        }

        public async Task<HttpResponseMessage?> GetAllSubscriptionProductsAsync(int? vertical = null,
            int? subvertical = null,
            int? productType = null)
        {
            try
            {
                var queryParams = new List<string>();

                if (vertical.HasValue)
                    queryParams.Add($"vertical={vertical.Value}");
                if (subvertical.HasValue)
                    queryParams.Add($"subvertical={subvertical.Value}");
                if (productType.HasValue)
                    queryParams.Add($"productType={productType.Value}");

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

                var requestUrl = $"/api/products/getallproducts{queryString}";

                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllSubscriptionProductsAsync");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
