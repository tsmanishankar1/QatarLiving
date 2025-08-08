

namespace QLN.DataMigration.Services
{
    public interface IMigrationService
    {
        Task<IResult> MigrateArticles(string sourceCategory, int destinationCategory, int destinationSubCategory, bool importImages, CancellationToken cancellationToken);
        Task<IResult> MigrateCategories(string environment, CancellationToken cancellationToken);
        Task<IResult> MigrateItems(string environment, int categoryId, CancellationToken cancellationToken);
    }
}
