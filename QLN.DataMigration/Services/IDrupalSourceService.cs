using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using QLN.DataMigration.Models;

namespace QLN.DataMigration.Services
{
    public interface IDrupalSourceService
    {
        Task<DrupalItems?> GetItemsAsync(string environment, int? page, int? page_size, string? type, CancellationToken cancellationToken);
        Task<DrupalItems?> GetServicesAsync(string environment, int? page, int? page_size, string? type, CancellationToken cancellationToken);
        Task<DrupalItemsMobileDevices?> GetMobileDevicesAsync(string environment, CancellationToken cancellationToken);
        Task<CommunitiesResponse?> GetCommunitiesFromDrupalAsync(CancellationToken cancellationToken, int? page = null, int? page_size = null);
        Task<ArticleResponse?> GetNewsFromDrupalAsync(string sourceCategory, CancellationToken cancellationToken, int? page = null, int? page_size = null);
        Task<ContentEventsResponse?> GetEventsFromDrupalAsync(CancellationToken cancellationToken, int? page = null, int? page_size = null);
        Task<CategoriesResponse?> GetCategoriesFromDrupalAsync(CancellationToken cancellationToken);
        Task<GetCommentsResponse?> GetCommentsByIdAsync(string postId, CancellationToken cancellationToken, int? page = null, int? page_size = null);
    }
}
