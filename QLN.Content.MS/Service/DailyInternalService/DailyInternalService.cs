using Dapr.Client;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Content.MS.Service.DailyInternalService
{
    public class DailyInternalService: IV2ContentDailyService

    {
        private readonly DaprClient _dapr;
        private readonly ILogger<IV2ContentDailyService> _logger;

        public DailyInternalService(DaprClient dapr, ILogger<IV2ContentDailyService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }
     

        public async Task AddDailyTopicAsync(DailyTopic topic, CancellationToken cancellationToken = default)
        {
            topic.Id = topic.Id == Guid.Empty ? Guid.NewGuid() : topic.Id;
            var key = $"daily-topic:{topic.Id}";

            // Save topic
            await _dapr.SaveStateAsync(V2Content.ContentStoreName, key, topic, cancellationToken: cancellationToken);

            // Maintain index
            var index = await _dapr.GetStateAsync<List<string>>(V2Content.ContentStoreName, V2Content.DailyTopicIndexKey, cancellationToken: cancellationToken)
                         ?? new List<string>();

            if (!index.Contains(key))
            {
                index.Add(key);
                await _dapr.SaveStateAsync(V2Content.ContentStoreName, V2Content.DailyTopicIndexKey, index, cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Daily topic {TopicName} with Id {Id} saved", topic.TopicName, topic.Id);
        }
    }
}
