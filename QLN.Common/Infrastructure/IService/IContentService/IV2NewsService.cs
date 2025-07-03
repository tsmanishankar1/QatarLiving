using QLN.Common.DTO_s;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IV2NewsService
    {
        Task<WriterTagsResponse> GetWriterTagsAsync(CancellationToken cancellationToken = default);
        Task<string> CreateNewsArticleAsync(string userId, V2NewsArticleDTO dto, CancellationToken cancellationToken = default);
        Task<List<V2NewsArticleDTO>> GetAllNewsArticlesAsync(CancellationToken cancellationToken = default);
        Task<List<V2NewsSlot>> GetAllSlotsAsync(CancellationToken cancellationToken = default);
        Task<List<V2NewsArticleDTO>> GetArticlesByCategoryIdAsync(int categoryId, CancellationToken cancellationToken);
        Task<List<V2NewsArticleDTO>> GetArticlesBySubCategoryIdAsync(int categoryId, int subCategoryId, CancellationToken cancellationToken);
        Task<string> UpdateNewsArticleAsync(V2NewsArticleDTO updatedDto, CancellationToken cancellationToken);

        // Filter articles by IsActive status 
        Task<List<V2NewsArticleDTO>> GetAllNewsFilterArticles(bool? isActive = null, CancellationToken cancellationToken = default);
        Task<string> DeleteNews(Guid id, CancellationToken cancellationToken = default);
        Task<string> ReorderSlotsAsync(ReorderSlotRequestDto dto, CancellationToken cancellationToken);
        Task<V2NewsArticleDTO?> GetArticleByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<V2NewsArticleDTO?> GetArticleBySlugAsync(string slug, CancellationToken cancellationToken);
        //category
        Task<List<V2NewsCategory>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
        Task<V2NewsCategory?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddCategoryAsync(V2NewsCategory category, CancellationToken cancellationToken = default);
        Task<bool> UpdateSubCategoryAsync(Guid categoryId, V2NewsSubCategory updatedSubCategory, CancellationToken cancellationToken = default);

    }

}
