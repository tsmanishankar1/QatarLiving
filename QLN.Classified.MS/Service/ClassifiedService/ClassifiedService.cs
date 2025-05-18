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

        public async Task<IEnumerable<ClassifiedIndexDto>> Search(CommonSearchRequest request)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("SearchAsync start");
            try
            {
                var common = await _dapr.InvokeMethodAsync<CommonSearchRequest, SearchResponse>(
                    SERVICE_APP_ID,
                    $"api/{Vertical}/search",
                    request
                );

                var items = common?.ClassifiedsItems?? Enumerable.Empty<ClassifiedIndexDto>();

                var adsOnly = items
                    .Where(i => string.Equals(
                        i.DocType,
                        ConstantValues.DocTypeAd,
                        StringComparison.OrdinalIgnoreCase
                    ));

                return adsOnly;
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
                var all = await GetAllItems(new CommonSearchRequest { Text = "*", Top = 1000 });
                var banners = all
                    .Where(i => string.Equals(i.DocType, ConstantValues.DocTypeBanner, StringComparison.OrdinalIgnoreCase)
                             && !string.IsNullOrWhiteSpace(i.BannerTitle)
                             && !string.IsNullOrWhiteSpace(i.BannerImageUrl))
                    .Select(i => new LandingBannerInfo
                    {
                        BannerTitle = i.BannerTitle!,
                        bannerUrl = i.BannerImageUrl!
                    })
                    .Distinct();
                var featuredItems = all
                    .Where(i => string.Equals(i.DocType, ConstantValues.DocTypeAd, StringComparison.OrdinalIgnoreCase)
                             && i.IsFeaturedItem == true);

                var featuredCategories = all
                    .Where(i => string.Equals(i.DocType, ConstantValues.DocTypeCategory, StringComparison.OrdinalIgnoreCase)
                             && i.IsFeaturedCategory == true
                             && !string.IsNullOrWhiteSpace(i.Category)
                             && !string.IsNullOrWhiteSpace(i.CategoryImageUrl))
                    .GroupBy(i => i.Category)
                    .Select(g => new LandingCategoryInfo
                    {
                        Category = g.Key,
                        ImageUrl = g.Select(x => x.CategoryImageUrl!)
                                    .First(url => !string.IsNullOrWhiteSpace(url))
                    });

                var featuredStoreGroups = all
                    .Where(i =>
                        string.Equals(i.DocType, ConstantValues.DocTypeStore, StringComparison.OrdinalIgnoreCase)
                        && i.IsFeaturedStore == true
                        && !string.IsNullOrWhiteSpace(i.StoreName)
                        && !string.IsNullOrWhiteSpace(i.StoreLogoUrl)
                    )
                    .GroupBy(i => i.StoreName!, StringComparer.OrdinalIgnoreCase);

                var featuredStores = featuredStoreGroups
                    .Select(g =>
                    {
                        var storeName = g.Key;
                        var logoUrl = g
                            .Select(x => x.StoreLogoUrl!)
                            .First(url => !string.IsNullOrWhiteSpace(url));

                        var itemCount = all.Count(i =>
                            string.Equals(i.DocType, ConstantValues.DocTypeAd, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(i.StoreName, storeName, StringComparison.OrdinalIgnoreCase)
                        );

                        return new LandingStoreInfo
                        {
                            StoreName = storeName,
                            LogoUrl = logoUrl,
                            ItemCount = itemCount
                        };
                    });

                var categoryCounts = all
                    .Where(i =>
                        string.Equals(i.DocType, ConstantValues.DocTypeCategory, StringComparison.OrdinalIgnoreCase)
                        && i.IsFeaturedCategory == false
                        && !string.IsNullOrWhiteSpace(i.Category)
                    )
                    .GroupBy(i => i.Category!, StringComparer.OrdinalIgnoreCase)
                    .Select(g =>
                    {
                        var catName = g.Key;
                        var imageUrl = g
                            .Select(x => x.CategoryImageUrl)
                            .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

                        var adCount = all
                            .Count(i =>
                                string.Equals(i.DocType, ConstantValues.DocTypeAd, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(i.Category, catName, StringComparison.OrdinalIgnoreCase)
                            );

                        return new CategoryAdCount
                        {
                            Category = catName,
                            ImageUrl = imageUrl,
                            Count = adCount
                        };
                    });

                return new ClassifiedLandingPageResponse
                {
                    ClassifiedBanners = banners,
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
        private async Task<IEnumerable<ClassifiedIndexDto>> GetAllItems(CommonSearchRequest request)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("SearchAsync start");
            try
            {
                var common = await _dapr.InvokeMethodAsync<CommonSearchRequest, SearchResponse>(
                    SERVICE_APP_ID,
                    $"api/{Vertical}/search",
                    request
                );

                var items = common?.ClassifiedsItems ?? Enumerable.Empty<ClassifiedIndexDto>();
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
    }
}
