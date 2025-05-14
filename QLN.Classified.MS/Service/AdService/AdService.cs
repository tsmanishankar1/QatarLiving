using Dapr.Client;
using QLN.Common.Infrastructure.IService.AdService;
using QLN.Common.Infrastructure.Model;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace QLN.Classified.MS.Service.AdService
{
    public class AdService : IAdService
    {
        private readonly DaprClient _dapr;
        private const string AdCategoryStore = "adcategorystore";
        private const string IndexKey = "adcategory-index";

        public AdService(DaprClient dapr)
        {
            _dapr = dapr;
        }

        public async Task<IEnumerable<AdCategory>> GetAllAdCategory()
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(AdCategoryStore, IndexKey) ?? new();
                if (!keys.Any()) return Enumerable.Empty<AdCategory>();

                var bulk = await _dapr.GetBulkStateAsync(AdCategoryStore, keys, parallelism: 10);
                var result = new List<AdCategory>();

                foreach (var item in bulk)
                {
                    if (!string.IsNullOrWhiteSpace(item.Value))
                    {
                        var category = JsonSerializer.Deserialize<AdCategory>(item.Value, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (category != null)
                            result.Add(category);
                    }
                }
                return result;
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        public async Task<AdCategory> AddAdCategory(AdCategory category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category.Key) || string.IsNullOrWhiteSpace(category.DisplayName))
                    throw new ArgumentException("Key and DisplayName are required.");

                var key = $"adcategory-{category.Key}";

                await _dapr.SaveStateAsync(AdCategoryStore, key, category);

                var indexKeys = await _dapr.GetStateAsync<List<string>>(AdCategoryStore, IndexKey) ?? new();
                if (!indexKeys.Contains(key))
                {
                    indexKeys.Add(key);
                    await _dapr.SaveStateAsync(AdCategoryStore, IndexKey, indexKeys);
                }

                return category;
            }
            catch(Exception ex)
            {
                throw;
            }
        }
    }
}
