using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IV2ContentDailyService
    {
        // existing topic methods...
        Task<string> CreateDailyTopicAsync(string userId, DailyTopSectionSlot dto, CancellationToken cancellationToken = default);
        Task<List<DailyTopicContent>> GetAllDailyTopicsAsync(CancellationToken cancellationToken = default);
        Task<DailyTopicContent?> GetDailyTopicByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<string> UpsertDailySlotAsync(
            string userId,
            Guid topicId,
            int slotNumber,
            DailyTopSectionSlot slotDto,
            CancellationToken cancellationToken = default
        );

        Task<List<DailyTopSectionSlot>> GetAllDailySlotsAsync(
            Guid topicId,
            CancellationToken cancellationToken = default
        );
    }
}
