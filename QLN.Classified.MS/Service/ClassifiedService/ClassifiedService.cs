using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.BannerService;

namespace QLN.Classified.MS.Service
{
    public class ClassifiedService : IClassifiedService
    {
        private const string SERVICE_APP_ID = ConstantValues.SearchServiceApp;
        private const string Vertical = ConstantValues.ClassifiedsVertical;

        private readonly DaprClient _dapr;
        private readonly ILogger<ClassifiedService> _logger;

        public ClassifiedService(DaprClient dapr, ILogger<ClassifiedService> logger)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ClassifiedIndexDto>> Search(ClassifiedSearchRequest request)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("SearchAsync start");
            try
            {
                var common = await _dapr.InvokeMethodAsync<ClassifiedSearchRequest, SearchResponse>(
                    SERVICE_APP_ID,
                    $"api/{Vertical}/search",
                    request
                );

                var items = common?.ClassifiedsItems?? Enumerable.Empty<ClassifiedIndexDto>();
                var json = JsonSerializer.Serialize(items);
                var dto = JsonSerializer.Deserialize<ClassifiedIndexDto[]>(
                                json,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                            );

                return dto ?? Array.Empty<ClassifiedIndexDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchAsync");
                throw;
            }
        }

        public async Task<ClassifiedIndexDto?> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id must be provided", nameof(id));

            _logger.LogInformation("GetByIdAsync start: id={Id}", id);
            try
            {
                var raw = await _dapr.InvokeMethodAsync<object>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/{id}"
                );

                var json = JsonSerializer.Serialize(raw);
                return JsonSerializer.Deserialize<ClassifiedIndexDto>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetByIdAsync for id={Id}", id);
                throw;
            }
        }

        public async Task<string> Upload(ClassifiedIndexDto document)
        {
            if (document is null) throw new ArgumentNullException(nameof(document));

            if (string.IsNullOrWhiteSpace(document.Id))
                document.Id = Guid.NewGuid().ToString();

            var req = new CommonIndexRequest
            {
                VerticalName = Vertical,
                ClassifiedsItem = document
            };

            try
            {
                return await _dapr.InvokeMethodAsync<CommonIndexRequest, string>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"/api/{Vertical}/upload",
                    req
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadAsync failed for id={Id}", document.Id);
                throw;
            }
        }

        public async Task<ClassifiedLandingPageResponse> GetLandingPage()
        {
            _logger.LogInformation("GetLandingPageAsync start");
            try
            {
                var all = await Search(new ClassifiedSearchRequest { Text = "*", Top = 1000 });

                var featuredItems = all.Where(i => i.IsFeaturedItem);

                var featuredCategories = all
                    .Where(i => i.IsFeaturedCategory
                             && !string.IsNullOrWhiteSpace(i.Category)
                             && !string.IsNullOrWhiteSpace(i.CategoryImageUrl))
                    .GroupBy(i => i.Category)
                    .Select(g => new LandingCategoryInfo
                    {
                        Category = g.Key,
                        ImageUrl = g.Select(x => x.CategoryImageUrl!).First(url => !string.IsNullOrWhiteSpace(url))
                    });

                var featuredStores = all
                    .Where(i => i.IsFeaturedStore
                             && !string.IsNullOrWhiteSpace(i.StoreName)
                             && !string.IsNullOrWhiteSpace(i.StoreLogoUrl))
                    .GroupBy(i => i.StoreName)
                    .Select(g => new LandingStoreInfo
                    {
                        StoreName = g.Key!,
                        LogoUrl = g.Select(x => x.StoreLogoUrl!).First(url => !string.IsNullOrWhiteSpace(url)),
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLandingPageAsync");
                throw;
            }
        }
    }
}
