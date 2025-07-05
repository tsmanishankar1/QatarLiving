using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IV2ContentDailyService
    {
        Task<string> CreateDailyTopicAsync(DailyTopSectionSlot dto, CancellationToken cancellationToken = default);
        Task<List<DailyTopSectionSlot>> GetAllDailyTopicsAsync(CancellationToken cancellationToken = default);
        Task<DailyTopSectionSlot?> GetDailyTopicByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<string> UpsertDailySlotAsync(Guid topicId,int slotNumber,DailyTopSectionSlot slotDto,CancellationToken cancellationToken = default);
        Task<List<DailyTopSectionSlot>> GetAllDailySlotsAsync(Guid topicId,CancellationToken cancellationToken = default);
        Task AddDailyTopicAsync(DailyTopic topic, CancellationToken cancellationToken = default);
    }
}
