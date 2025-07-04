using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.Backend.API.Service.V2ContentService
{
    public class V2ExternalDailyService : IV2ContentDailyService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<V2ExternalDailyService> _logger;
        private readonly IFileStorageBlobService _blobStorage;

        public V2ExternalDailyService(
           DaprClient dapr,
           ILogger<V2ExternalDailyService> logger,
           IFileStorageBlobService blobStorage)
        {
            _dapr = dapr;
            _logger = logger;
            _blobStorage = blobStorage;
        }

        public async Task AddDailyTopicAsync(DailyTopic topic, CancellationToken cancellationToken = default)
        {
            try
            {
                var path = "/api/v2/daily/dailytopicById";
                var appId = ConstantValues.V2Content.ContentServiceAppId;

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, appId, path);
                request.Content = new StringContent(JsonSerializer.Serialize(topic), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to create topic. Status: {Status}, Content: {Body}", response.StatusCode, responseBody);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create DailyTopic");
                throw;
            }
        }




    }
}
