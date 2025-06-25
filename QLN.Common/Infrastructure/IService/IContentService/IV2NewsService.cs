using QLN.Common.DTO_s;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IV2NewsService
    {
        ///////// New News Json 
        Task<Dictionary<string, string>> GetWriterTagsAsync(CancellationToken cancellationToken = default);
        Task<CreateNewsArticleResponseDto> CreateNewsArticleAsync(Guid userId, V2NewsArticleDTO dto, CancellationToken cancellationToken = default);
        Task<List<V2NewsArticleDTO>> GetAllNewsArticlesAsync(CancellationToken cancellationToken = default);
        Task<List<V2NewsCategory>> GetNewsCategoriesAsync(CancellationToken cancellationToken = default);
        Task<List<V2Slot>> GetAllSlotsAsync(CancellationToken cancellationToken = default);
        Task<List<V2NewsArticleDTO>> GetArticlesByCategoryIdAsync(int categoryId, CancellationToken cancellationToken);
        Task<List<V2NewsArticleDTO>> GetArticlesBySubCategoryIdAsync(int categoryId, int subCategoryId, CancellationToken cancellationToken);
        Task<string> UpdateNewsArticleAsync(V2NewsArticleDTO updatedDto, CancellationToken cancellationToken);
    }
}
