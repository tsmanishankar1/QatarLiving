using QLN.DataMigration.Models;

namespace QLN.DataMigration.Services
{
    public interface IDataOutputService
    {
        Task SaveCategoriesAsync(ItemsCategories itemsCategories);
        Task SaveMigrationItemsAsync(List<MigrationItem> migrationItems);
    }
}
