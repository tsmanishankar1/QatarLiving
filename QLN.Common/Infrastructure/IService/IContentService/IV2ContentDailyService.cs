using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IV2ContentDailyService
    {
        Task<string> UpsertSlotAsync(string userId, DailyTopSectionSlot dto,CancellationToken cancellationToken = default);
        Task<List<DailyTopSectionSlot>> GetAllSlotsAsync(CancellationToken cancellationToken = default);
        Task<string> CreateContentAsync(string userId, DailyTopicContent dto, CancellationToken ct);
        Task<string> ReorderSlotsAsync(string userId, ReorderDailyTopicContentDto dto, CancellationToken ct);
        Task<string> DeleteContentAsync(Guid contentId, CancellationToken ct);
        Task<List<DailyTopicContent>> GetSlotsByTopicAsync(Guid topicId, CancellationToken ct = default);
    }
}
