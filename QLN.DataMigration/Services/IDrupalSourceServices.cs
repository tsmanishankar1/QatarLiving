using QLN.DataMigration.Models;

namespace QLN.DataMigration.Services
{
    public interface IDrupalSourceServices
    {
        Task<DrupalItemsCategories?> GetCategoriesAsync(string environment);
        Task<DrupalItems?> GetItemsAsync(string environment, int categoryId, string sortField, string sortOrder, string keywords, int pageSize, int page);
    }
}
