using QLN.Common.DTO_s;

namespace QLN.Common.Infrastructure.IService.V2IContent
{
    public interface IV2ContentNews
    {
        Task<NewsSummary> ProcessNewsContentAsync(ContentNewsDto dto, string userId, CancellationToken cancellationToken = default);
    }
}
