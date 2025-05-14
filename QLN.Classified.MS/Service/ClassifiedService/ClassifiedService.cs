using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapr.Client;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.BannerService;
using QLN.Common.Infrastructure.Model;

namespace QLN.Classified.MS.Service
{
    public class ClassifiedService : IClassifiedService
    {
        private readonly DaprClient _dapr;
        private const string SearchAppId = "searchservice";

        public ClassifiedService(DaprClient dapr)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
        }

        /// <summary>
        /// Executes a search on the external SearchService via Dapr.
        /// </summary>
        public async Task<IEnumerable<ClassifiedIndexDto>> SearchAsync(
            string vertical,
            ClassifiedSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical cannot be null or empty.", nameof(vertical));

            var path = $"api/{vertical}/search";
            var raw = await _dapr.InvokeMethodAsync<ClassifiedSearchRequest, object>(
                SearchAppId,
                path,
                request);

            var items = JsonSerializer.Deserialize<ClassifiedIndexDto[]>(
                JsonSerializer.Serialize(raw),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            return items ?? Array.Empty<ClassifiedIndexDto>();
        }

        /// <summary>
        /// Retrieves a single ad by its ID.
        /// </summary>
        public async Task<ClassifiedIndexDto?> GetByIdAsync(
                   string vertical,
                   string id)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical cannot be null or empty", nameof(vertical));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be null or empty", nameof(id));

            // no leading slash, GET path
            var path = $"api/{vertical}/{id}";

            // ← Use the GET overload
            var raw = await _dapr.InvokeMethodAsync<object>(
                HttpMethod.Get,
                SearchAppId,
                path
            );

            // re-serialize into your DTO
            return JsonSerializer.Deserialize<ClassifiedIndexDto>(
                JsonSerializer.Serialize(raw),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );
        }

        /// <summary>
        /// Uploads or updates an ad in the SearchService index.
        /// </summary>
        public async Task<string> UploadAsync(
            string vertical,
            ClassifiedIndexDto document)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical cannot be null or empty.", nameof(vertical));
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var path = $"api/{vertical}/upload";
            return await _dapr.InvokeMethodAsync<ClassifiedIndexDto, string>(
                SearchAppId,
                path,
                document
            );
        }

        /// <summary>
        /// Builds the landing-page model by querying all items and grouping.
        /// </summary>
        public async Task<ClassifiedLandingPageResponse> GetLandingPageAsync(
            string vertical)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical cannot be null or empty.", nameof(vertical));

            // Fetch up to 1000 items
            var all = await SearchAsync(vertical, new ClassifiedSearchRequest { Text = "*", Top = 1000 });

            var featuredItems = all.Where(i => i.IsFeaturedItem);
            var featuredCategories = all
                .Where(i => i.IsFeaturedCategory)
                .GroupBy(i => i.Category)
                .Select(g => new LandingCategoryInfo
                {
                    Category = g.Key,
                    ImageUrl = g.First().StoreLogoUrl
                });

            var featuredStores = all
                .Where(i => i.IsFeaturedStore)
                .GroupBy(i => i.StoreName)
                .Select(g => new LandingStoreInfo
                {
                    StoreName = g.Key,
                    LogoUrl = g.First().StoreLogoUrl,
                    ItemCount = g.Count()
                });

            var categoryCounts = all
                .GroupBy(i => i.Category)
                .Select(g => new CategoryAdCount
                {
                    Category = g.Key,
                    Count = g.Count()
                });

            return new ClassifiedLandingPageResponse
            {
                FeaturedItems = featuredItems,
                FeaturedCategories = featuredCategories,
                FeaturedStores = featuredStores,
                CategoryAdCounts = categoryCounts
            };
        }
    }
}