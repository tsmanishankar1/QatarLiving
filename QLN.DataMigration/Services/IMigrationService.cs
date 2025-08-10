

namespace QLN.DataMigration.Services
{
    public interface IMigrationService
    {
        Task<IResult> MigrateArticles(string sourceCategory, int destinationCategory, int destinationSubCategory, bool importImages, CancellationToken cancellationToken);
        Task<IResult> MigrateCategories(string environment, CancellationToken cancellationToken);
        Task<IResult> MigrateCommunityPosts(bool importImages, CancellationToken cancellationToken);
        Task<IResult> MigrateEvents(string sourceCategory, int destinationCategory, bool importImages, CancellationToken cancellationToken);
        Task<IResult> MigrateItems(string environment, int categoryId, CancellationToken cancellationToken);

        Task<IResult> MigrateEventCategories(CancellationToken cancellationToken);
        Task<IResult> MigrateNewsCategories(CancellationToken cancellationToken);
        Task<IResult> MigrateLocations(CancellationToken cancellationToken);
    }
}
