using QLN.DataMigration.Models;

namespace QLN.DataMigration.Services
{
    public interface IMigrationService
    {
        Task SaveCategoriesAsync(ItemsCategories itemsCategories);
        Task SaveMigrationItemsAsync(MigrationItems migrationItems);
    }
}
