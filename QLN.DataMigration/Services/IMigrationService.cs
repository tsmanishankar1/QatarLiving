
namespace QLN.DataMigration.Services
{
    public interface IMigrationService
    {
        Task<IResult> MigrateCategories(string environment);
        Task<IResult> MigrateItems(string environment, int categoryId);
    }
}
