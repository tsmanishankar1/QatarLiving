// QLN.Content.MS/Service/DailyInternalService/DailyInternalService.cs

using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.Constants;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Content.MS.Service.DailyInternalService
{
    public class DailyInternalService : IV2ContentDailyService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<DailyInternalService> _logger;
        private const string Store = ConstantValues.V2Content.ContentStoreName;

        public DailyInternalService(DaprClient dapr, ILogger<DailyInternalService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }

        public async Task<string> CreateDailyTopicAsync(
            string userId,
            DailyTopSectionSlot dto,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                dto.Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;
                await _dapr.SaveStateAsync(ConstantValues.V2Content.ContentStoreName, dto.Id.ToString(), dto, cancellationToken: cancellationToken);

                var currentIndex = await _dapr.GetStateAsync<List<string>>(ConstantValues.V2Content.ContentStoreName, ConstantValues.V2Content.DailyTopBOIndexKey)
                                   ?? new List<string>();

                if (!currentIndex.Contains(dto.Id.ToString()))
                {
                    currentIndex.Add(dto.Id.ToString());
                    await _dapr.SaveStateAsync(ConstantValues.V2Content.ContentStoreName, ConstantValues.V2Content.DailyTopBOIndexKey, currentIndex, cancellationToken: cancellationToken);
                }

                return "Daily topic created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating daily topic");
                throw;
            }
        }

        public async Task<List<DailyTopicContent>> GetAllDailyTopicsAsync(
           CancellationToken cancellationToken = default
        )
        {
            var currentIndex = await _dapr.GetStateAsync<List<string>>(ConstantValues.V2Content.ContentStoreName, ConstantValues.V2Content.DailyTopBOIndexKey)
                               ?? new List<string>();

            if (currentIndex.Count == 0)
                return new List<DailyTopicContent>();

            var bulk = await _dapr.GetBulkStateAsync<DailyTopicContent>(ConstantValues.V2Content.ContentStoreName, currentIndex, parallelism: 10, cancellationToken: cancellationToken);
            return bulk
                  .Where(item => item.Value is not null)
                  .Select(item => item.Value!)
                  .ToList();
        }

        public Task<DailyTopicContent?> GetDailyTopicByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default
        ) => _dapr.GetStateAsync<DailyTopicContent>(ConstantValues.V2Content.ContentStoreName, id.ToString(), cancellationToken: cancellationToken);

        public async Task<string> UpsertDailySlotAsync(
            string userId,
            Guid topicId,
            int slotNumber,
            DailyTopSectionSlot slotDto,
            CancellationToken cancellationToken = default
        )
        {
            // 1. enforce exactly 1–9
            if (slotNumber < 1 || slotNumber > 9)
                throw new ArgumentOutOfRangeException(nameof(slotNumber), "Slot must be 1–9");

            // 2. enforce content-type by slot
            var slotType = (DailySlotType)slotNumber;
            if (slotType == DailySlotType.TopStory && slotDto.ContentType != DailyContentType.Article)
                throw new InvalidOperationException("TopStory must be an Article");
            if (slotType == DailySlotType.HighlightedEvent && slotDto.ContentType != DailyContentType.Event)
                throw new InvalidOperationException("HighlightedEvent must be an Event");
            if (slotNumber >= 3 && slotNumber <= 9 && slotDto.ContentType != DailyContentType.Article)
                throw new InvalidOperationException("Articles slots must be Article");

            // 3. build metadata
            slotDto.Id = Guid.NewGuid();
            slotDto.SlotType = slotType;
            slotDto.SlotNumber = slotNumber;
            slotDto.CreatedBy = userId;
            slotDto.CreatedAt = DateTime.UtcNow;
            slotDto.UpdatedBy = userId;
            slotDto.UpdatedAt = DateTime.UtcNow;

            // 4. save under fixed key
            var key = $"daily-topic-{topicId}-slot-{slotNumber}";
            await _dapr.SaveStateAsync(Store, key, slotDto, cancellationToken: cancellationToken);

            return "Slot upserted";
        }

        public async Task<List<DailyTopSectionSlot>> GetAllDailySlotsAsync(
            Guid topicId,
            CancellationToken cancellationToken = default
        )
        {
            var tasks = Enumerable.Range(1, 9)
                .Select(async slotNum =>
                {
                    var key = $"daily-topic-{topicId}-slot-{slotNum}";
                    return await _dapr.GetStateAsync<DailyTopSectionSlot>(Store, key, cancellationToken: cancellationToken);
                });

            var results = await Task.WhenAll(tasks);
            // skip nulls or return empty slots as you prefer
            return results.Where(x => x is not null)!.Cast<DailyTopSectionSlot>().ToList();
        }
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
