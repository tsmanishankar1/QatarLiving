using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using QLN.DataMigration.Models;

namespace QLN.DataMigration.Services
{
    public interface IDataOutputService
    {
        Task SaveCategoriesAsync(ItemsCategories itemsCategories, CancellationToken cancellationToken);
        Task SaveMigrationItemsAsync(List<MigrationItem> migrationItems, CancellationToken cancellationToken);
        Task SaveContentNewsAsync(List<ArticleItem> items, int categoryId, int subcategoryId, CancellationToken cancellationToken);
        Task SaveContentEventsAsync(List<ContentEvent> items, int destinationCategoryId, CancellationToken cancellationToken);
        Task SaveContentCommunityPostsAsync(List<CommunityPost> items, CancellationToken cancellationToken);
    }
}
