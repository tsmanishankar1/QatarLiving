using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IV2NewsService
    {
        Task<string> CreateWritertagAsync(WritertagDTO dto, CancellationToken cancellationToken);
        Task<List<WritertagDTO>> GetAllWritertagsAsync(CancellationToken cancellationToken);
        Task<string> Deletetagname(Guid id, CancellationToken cancellationToken = default);

        Task<WriterTagsResponse> GetWriterTagsAsync(CancellationToken cancellationToken = default);
        Task<string> CreateNewsArticleAsync(string userId, V2NewsArticleDTO dto, CancellationToken cancellationToken = default);
        Task<PagedResponse<V2NewsArticleDTO>> GetAllNewsArticlesAsync(
           int? page, int? perPage, string? search, CancellationToken cancellationToken = default);
        Task<List<V2NewsSlot>> GetAllSlotsAsync(CancellationToken cancellationToken = default);
        Task<List<V2NewsArticleDTO>> GetArticlesByCategoryIdAsync(int categoryId, CancellationToken cancellationToken);

            Task<List<V2NewsArticleDTO>> GetArticlesBySubCategoryIdAsync(
                int categoryId,
                int subCategoryId,
                ArticleStatus status,
                int? page,
                int? pageSize,
                CancellationToken cancellationToken);


        Task<string> UpdateNewsArticleAsync(V2NewsArticleDTO updatedDto, CancellationToken cancellationToken);

        // Filter articles by IsActive status 
        Task<List<V2NewsArticleDTO>> GetAllNewsFilterArticles(bool? isActive = null, CancellationToken cancellationToken = default);
        Task<string> DeleteNews(Guid id, CancellationToken cancellationToken = default);
        Task<string> ReorderSlotsAsync(NewsSlotReorderRequest dto, CancellationToken cancellationToken);
        Task<V2NewsArticleDTO?> GetArticleByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<V2NewsArticleDTO?> GetArticleBySlugAsync(string slug, CancellationToken cancellationToken);
        //category
        Task<List<V2NewsCategory>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
        Task<V2NewsCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
        Task AddCategoryAsync(V2NewsCategory category, CancellationToken cancellationToken = default);
        Task<bool> UpdateSubCategoryAsync(int categoryId, V2NewsSubCategory updatedSubCategory, CancellationToken cancellationToken = default);
        Task<NewsCommentApiResponse> SaveNewsCommentAsync(V2NewsCommentDto dto, CancellationToken ct = default);
        Task<NewsCommentListResponse> GetCommentsByArticleIdAsync(string nid, int? page = null, int? perPage = null, CancellationToken ct = default);
        Task<bool> LikeNewsCommentAsync(string commentId, string userId, string userName, CancellationToken ct = default);       
        Task<NewsCommentApiResponse> SoftDeleteNewsCommentAsync(string articleId, Guid commentId, string userId, CancellationToken ct = default);
        Task<NewsCommentApiResponse> EditNewsCommentAsync(string articleId, Guid commentId, string userId, string updatedText, CancellationToken ct = default);
        Task<GenericNewsPageResponse> GetNewsLandingPageAsync(
       int categoryId,
       int subCategoryId,
       CancellationToken cancellationToken = default
   );
    }

}
