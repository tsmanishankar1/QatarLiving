using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using QLN.DataMigration.Models;

namespace QLN.DataMigration.Services
{
    public interface IDrupalSourceService
    {
        Task<DrupalItemsCategories?> GetCategoriesAsync(string environment, CancellationToken cancellationToken);
        Task<DrupalItems?> GetItemsAsync(
            string environment,
            //int categoryId,
            //string sortField,
            //string sortOrder,
            //string? keywords,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken);
        Task<CommunitiesResponse?> GetCommunitiesFromDrupalAsync(CancellationToken cancellationToken, int? page = null, int? page_size = null);
        Task<ArticleResponse?> GetNewsFromDrupalAsync(string sourceCategory, CancellationToken cancellationToken, int? page = null, int? page_size = null);
        Task<ContentEventsResponse?> GetEventsFromDrupalAsync(CancellationToken cancellationToken, int? page = null, int? page_size = null);
        Task<CategoriesResponse?> GetCategoriesFromDrupalAsync(CancellationToken cancellationToken);
    }
}
