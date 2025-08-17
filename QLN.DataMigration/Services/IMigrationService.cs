

namespace QLN.DataMigration.Services
{
    public interface IMigrationService
    {
        Task<IResult> MigrateMobileDevices(string environment, CancellationToken cancellationToken);
        Task<IResult> MigrateItems(string environment, bool importImages, CancellationToken cancellationToken);
        Task<IResult> MigrateArticles(bool importImages, CancellationToken cancellationToken);
        Task<IResult> MigrateEvents(bool importImages, CancellationToken cancellationToken);
        Task<IResult> MigrateCommunityPosts(bool importImages, CancellationToken cancellationToken);
        Task<IResult> MigrateEventCategories(CancellationToken cancellationToken);
        Task<IResult> MigrateNewsCategories(CancellationToken cancellationToken);
        Task<IResult> MigrateLocations(CancellationToken cancellationToken);
        Task<IResult> MigrateLegacyItemsSubscriptions(CancellationToken cancellationToken = default);
        Task<IResult> MigrateLegacyServicesSubscriptions(CancellationToken cancellationToken = default);
    }
}
