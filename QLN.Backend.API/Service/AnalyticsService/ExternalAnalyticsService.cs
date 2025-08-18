using Dapr;
using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.ISearchService;
using System.Net.Http;

namespace QLN.Backend.API.Service.AnalyticsService
{
    public class ExternalAnalyticsService : IAnalyticsService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalAnalyticsService> _logger;
        private readonly string SERVICE_APP_ID = ConstantValues.ServiceAppIds.SearchServiceApp;
        private readonly HttpClient _httpClient;

        public ExternalAnalyticsService(
            DaprClient dapr,
            ILogger<ExternalAnalyticsService> logger,
            HttpClient httpClient)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient;
        }

        public async Task<AnalyticsIndex?> GetAsync(string section, string entityId)
        {
            if (string.IsNullOrWhiteSpace(section))
                throw new ArgumentException("Section is required.", nameof(section));
            if (string.IsNullOrWhiteSpace(entityId))
                throw new ArgumentException("EntityId is required.", nameof(entityId));

            try
            {
                return await _dapr.InvokeMethodAsync<AnalyticsIndex?>(
                    HttpMethod.Get,
                    appId: SERVICE_APP_ID,
                    methodName: $"api/analytics/getAnalytics/{section}/{entityId}"
                );
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex,
                    "GetAsync called with null/empty parameter: {ParamName}",
                    ex.ParamName);
                throw;
            }
            catch (DaprException ex)
            {
                _logger.LogError(ex,
                    "Dapr call failed for GetAnalytics: section={Section}, entityId={EntityId}",
                    section, entityId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error in GetAnalytics: section={Section}, entityId={EntityId}",
                    section, entityId);
                throw;
            }
        }

        public async Task UpsertAsync(AnalyticsEventRequest req)
        {
            if (req == null)
                throw new ArgumentNullException(nameof(req));

            try
            {
                await _dapr.InvokeMethodAsync(
                    HttpMethod.Post,
                    appId: SERVICE_APP_ID,
                    methodName: "api/analytics/upsertAnalytics",
                    req
                );
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex,
                    "UpsertAsync called with null AnalyticsEventRequest.");
                throw;
            }
            catch (DaprException ex)
            {
                _logger.LogError(ex,
                    "Dapr call failed for UpsertAnalytics: {@Request}",
                    req);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error in UpsertAnalytics: {@Request}",
                    req);
                throw;
            }
        }
        public async Task<ApiResponse<object>> GetAnalyticsAsync(AnalyticsRequestDto request, string uid, CancellationToken cancellationToken = default)
        {
            try
            {
                var keyValues = new List<KeyValuePair<string, string>>
                {
                    new("type", request.Type),
                    new("uid", uid),
                    new("start", request.Start),
                    new("end", request.End)
                };

                using var content = new FormUrlEncodedContent(keyValues);

                var response = await _httpClient.PostAsync("qlnapi/get/analytics", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync(cancellationToken);
                var json = System.Text.Json.JsonSerializer.Deserialize<object>(result);

                return new ApiResponse<object>
                {
                    Status = true,
                    Message = $"Analytics data retrieved successfully for {request.Type}",
                    Data = json
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling analytics service");
                return new ApiResponse<object>
                {
                    Status = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetAnalyticsAsync");
                return new ApiResponse<object>
                {
                    Status = false,
                    Message = "An unexpected error occurred"
                };
            }
        }
    }
}
