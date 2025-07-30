using QLN.DataMigration.Models;

namespace QLN.DataMigration.Services
{
    public interface IDrupalSourceService
    {
        Task<DrupalItemsCategories?> GetCategoriesAsync(string environment);
        Task<DrupalItems?> GetItemsAsync(
            string environment,
            int categoryId,
            string sortField,
            string sortOrder,
            string? keywords,
            int? page,
            int? pageSize);
    }
}
