using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using QLN.DataMigration.Models;

namespace QLN.DataMigration.Services
{
    public interface IDataOutputService
    {
        Task SaveCategoriesAsync(ItemsCategories itemsCategories, CancellationToken cancellationToken);
        Task SaveMigrationItemsAsync(List<CsvCategoryMapper> csvImport, List<DrupalItem> migrationItems, CancellationToken cancellationToken);
        Task SaveContentNewsAsync(List<ArticleItem> items, int categoryId, int subcategoryId, CancellationToken cancellationToken);
        Task SaveContentEventsAsync(List<ContentEvent> items, CancellationToken cancellationToken);
        Task SaveContentCommunityPostsAsync(List<CommunityPost> items, CancellationToken cancellationToken);

        Task SaveEventCategoriesAsync(List<Common.Infrastructure.DTO_s.EventCategory> items, CancellationToken cancellationToken);
        Task SaveNewsCategoriesAsync(List<NewsCategory> items, CancellationToken cancellationToken);
        Task SaveLocationsAsync(List<Location> items, CancellationToken cancellationToken);
        Task SaveContentCommunityCommentsAsync(Dictionary<string, List<ContentComment>> items, CancellationToken cancellationToken);
        Task SaveLegacyServicesSubscriptionsAsync(List<SubscriptionItem> subscriptions, CancellationToken cancellationToken);
        Task SaveLegacyItemsSubscriptionsAsync(List<SubscriptionItem> subscriptions, CancellationToken cancellationToken);
    }
}
