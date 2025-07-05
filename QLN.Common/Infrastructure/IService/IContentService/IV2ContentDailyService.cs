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
    }
}
