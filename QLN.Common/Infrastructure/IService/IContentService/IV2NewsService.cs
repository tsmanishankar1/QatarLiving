using QLN.Common.DTO_s;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IV2NewsService
    {
        Task<Dictionary<string, string>> GetWriterTagsAsync(CancellationToken cancellationToken = default);
        Task<string> CreateNewsArticleAsync(string userId, V2NewsArticleDTO dto, CancellationToken cancellationToken = default); Task<List<V2NewsArticleDTO>> GetAllNewsArticlesAsync(CancellationToken cancellationToken = default);
        Task<List<V2NewsCategory>> GetNewsCategoriesAsync(CancellationToken cancellationToken = default);
        Task<List<V2NewsSlot>> GetAllSlotsAsync(CancellationToken cancellationToken = default);
        Task<List<V2NewsArticleDTO>> GetArticlesByCategoryIdAsync(int categoryId, CancellationToken cancellationToken);
        Task<List<V2NewsArticleDTO>> GetArticlesBySubCategoryIdAsync(int categoryId, int subCategoryId, CancellationToken cancellationToken);
        Task<string> UpdateNewsArticleAsync(V2NewsArticleDTO updatedDto, CancellationToken cancellationToken);
    }
}
