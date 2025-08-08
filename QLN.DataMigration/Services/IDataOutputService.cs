using QLN.Common.DTO_s;
using QLN.DataMigration.Models;

namespace QLN.DataMigration.Services
{
    public interface IDataOutputService
    {
        Task SaveCategoriesAsync(ItemsCategories itemsCategories, CancellationToken cancellationToken);
        Task SaveMigrationItemsAsync(List<MigrationItem> migrationItems, CancellationToken cancellationToken);
        Task SaveContentNewsAsync(List<ArticleItem> items, int categoryId, int subcategoryId, CancellationToken cancellationToken);
    }
}
