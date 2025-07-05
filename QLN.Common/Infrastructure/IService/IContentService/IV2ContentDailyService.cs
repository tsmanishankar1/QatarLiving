using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IV2ContentDailyService
    {
        Task<string> UpsertSlotAsync(DailyTopSectionSlot dto,CancellationToken cancellationToken = default);
        Task<List<DailyTopSectionSlot>> GetAllSlotsAsync(CancellationToken cancellationToken = default);
        Task AddDailyTopicAsync(DailyTopic topic, CancellationToken cancellationToken = default);
        Task<List<DailyTopic>> GetAllDailyTopicsAsync(CancellationToken cancellationToken = default);
        Task<bool> SoftDeleteDailyTopicAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> UpdateDailyTopicAsync(DailyTopic topic, CancellationToken cancellationToken = default);
        Task<bool> UpdatePublishStatusAsync(Guid id, bool isPublished, CancellationToken cancellationToken = default);

    }
}
