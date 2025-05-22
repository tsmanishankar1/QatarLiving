using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.BannerService;
using QLN.Common.Infrastructure.Model;
using static Dapr.Client.Autogen.Grpc.v1.Dapr;

namespace QLN.Classified.MS.Service
{
    public class ClassifiedService : IClassifiedService
    {
        private const string SERVICE_APP_ID = ConstantValues.SearchServiceApp;
        private const string Vertical = ConstantValues.ClassifiedsVertical;
        private readonly IWebHostEnvironment _env;
        private readonly Dapr.Client.DaprClient _dapr;        
        private readonly IBannerService _bannerService;

        private const string UnifiedStore = "adstore";
        private const string UnifiedIndexKey = "ad-index";               
        private readonly ILogger<ClassifiedService> _logger;

        public ClassifiedService(Dapr.Client.DaprClient dapr, ILogger<ClassifiedService> logger, IWebHostEnvironment env, IBannerService bannerService)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bannerService = bannerService ?? throw new ArgumentNullException(nameof(bannerService));
            _env = env;
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

        public async Task<T> AddItem<T>(T item) where T : BaseItem
        {
            item.Id = Guid.NewGuid();
            var key = $"{item.Type.ToString().ToLowerInvariant()}-{item.Id}";

            var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, UnifiedIndexKey) ?? new();
            await _dapr.SaveStateAsync(UnifiedStore, key, item);

            index.Add(key);
            await _dapr.SaveStateAsync(UnifiedStore, UnifiedIndexKey, index);

            return item;
        }

        public async Task<List<T>> GetItemsByType<T>(AdItemType type) where T : BaseItem
        {
            var keys = await _dapr.GetStateAsync<List<string>>(UnifiedStore, UnifiedIndexKey) ?? new();
            var result = new List<T>();

            foreach (var key in keys)
            {
                if (!key.StartsWith($"{type.ToString().ToLowerInvariant()}-")) continue;

                var item = await _dapr.GetStateAsync<T>(UnifiedStore, key);
                if (item != null) result.Add(item);
            }

            return result;
        }

        public async Task<Adcateg> AddCategory(string categoryName, CancellationToken cancellationToken = default)
        {
            try
            {
                var category = new Adcateg
                {
                    Name = categoryName,
                    Type = AdItemType.Category
                };
                return await AddItem(category);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while adding category", ex);
            }
        }

        public async Task<List<Adcateg>> GetAllCategories(CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetItemsByType<Adcateg>(AdItemType.Category);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all categories", ex);
            }
        }

        public async Task<AdSubCategory> AddSubCategory(string name, Guid categoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                var subcategory = new AdSubCategory
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    CategoryId = categoryId,
                    Type = AdItemType.SubCategory
                };

                var key = $"subcategory-{subcategory.Id}";

                await _dapr.SaveStateAsync(UnifiedStore, key, subcategory);

                var keys = await _dapr.GetStateAsync<List<string>>(UnifiedStore, UnifiedIndexKey) ?? new();
                keys.Add(key);
                await _dapr.SaveStateAsync(UnifiedStore, UnifiedIndexKey, keys);

                return subcategory;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while adding subcategory", ex);
            }
        }

        public async Task<List<AdSubCategory>> GetAllSubCategories(CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(UnifiedStore, UnifiedIndexKey) ?? new();
                var result = new List<AdSubCategory>();

                foreach (var key in keys)
                {
                    if (!key.StartsWith("subcategory-")) continue;

                    var item = await _dapr.GetStateAsync<AdSubCategory>(UnifiedStore, key);
                    if (item != null && item.Type == AdItemType.SubCategory)
                    {
                        result.Add(item);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all subcategories", ex);
            }
        }

        public async Task<List<AdSubCategory>> GetSubCategoriesByCategoryId(Guid categoryId)
        {
            try
            {
                var all = await GetItemsByType<AdSubCategory>(AdItemType.SubCategory);
                return all.Where(s => s.CategoryId == categoryId).ToList();
            }
            catch(Exception ex)
            {
                throw new Exception("Error while getting subcategories by category id", ex);
            }
        }

        public async Task<AdBrand> AddBrand(string name, Guid subCategoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                var brand = new AdBrand
                {
                    Name = name,
                    SubCategoryId = subCategoryId,
                    Type = AdItemType.Brand
                };

                return await AddItem(brand);
            }
            catch(Exception ex)
            {
                throw new Exception("Error while adding brand", ex);
            }
        }

        public async Task<List<AdBrand>> GetAllBrands(CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetItemsByType<AdBrand>(AdItemType.Brand);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all brands", ex);
            }
        }

        public async Task<List<AdBrand>> GetBrandsBySubCategoryId(Guid subCategoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                var allBrands = await GetItemsByType<AdBrand>(AdItemType.Brand);
                return allBrands.Where(b => b.SubCategoryId == subCategoryId).ToList();
            }
            catch(Exception ex)
            {
                throw new Exception("Error while getting brands by subcategory id", ex);
            }
        }

        public async Task<AdModel> AddModel(string name, Guid brandId, CancellationToken cancellationToken = default)
        {
            try
            {
                var model = new AdModel
                {
                    Name = name,
                    BrandId = brandId,
                    Type = AdItemType.Model
                };

                return await AddItem(model);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while adding model", ex);
            }
        }

        public async Task<List<AdModel>> GetAllModels(CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetItemsByType<AdModel>(AdItemType.Model);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all models", ex);
            }
        }

        public async Task<List<AdModel>> GetModelsByBrandId(Guid brandId, CancellationToken cancellationToken = default)
        {
            try
            {
                var allModels = await GetItemsByType<AdModel>(AdItemType.Model);
                return allModels.Where(m => m.BrandId == brandId).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting models by brand ID", ex);
            }
        }

        public async Task<AdCondition> AddCondition(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var condition = new AdCondition
                {
                    Name = name,
                    Type = AdItemType.Condition
                };

                return await AddItem(condition);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while adding condition", ex);
            }
        }

        public async Task<List<AdCondition>> GetAllConditions(CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetItemsByType<AdCondition>(AdItemType.Condition);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting conditions", ex);
            }
        }

        public async Task<AdColor> AddColor(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var color = new AdColor
                {
                    Name = name,
                    Type = AdItemType.Color
                };

                return await AddItem(color);
            }
            catch(Exception ex)
            {
                throw new Exception("Error while adding color", ex);
            }
        }

        public async Task<List<AdColor>> GetAllColors(CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetItemsByType<AdColor>(AdItemType.Color);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all colors", ex);
            }
        }

        public async Task<AdCapacity> AddCapacity(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var capacity = new AdCapacity
                {
                    Name = name,
                    Type = AdItemType.Capacity
                };

                return await AddItem(capacity);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while adding capacity", ex);
            }
        }

        public async Task<List<AdCapacity>> GetAllCapacities(CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetItemsByType<AdCapacity>(AdItemType.Capacity);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all capacities", ex);
            }
        }

        public async Task<AdProcessor> AddProcessor(string name, Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var processor = new AdProcessor
                {
                    Name = name,
                    ModelId = modelId,
                    Type = AdItemType.Processor
                };

                return await AddItem(processor);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while adding processor", ex);
            }
        }

        public async Task<List<AdProcessor>> GetAllProcessors(CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetItemsByType<AdProcessor>(AdItemType.Processor);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting processors", ex);
            }
        }

        public async Task<List<AdProcessor>> GetProcessorsByModelId(Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var all = await GetItemsByType<AdProcessor>(AdItemType.Processor);
                return all.Where(p => p.ModelId == modelId).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting processors by model ID", ex);
            }
        }

        public async Task<AdCoverage> AddCoverage(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var coverage = new AdCoverage
                {
                    Name = name,
                    Type = AdItemType.Coverage
                };

                return await AddItem(coverage);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while adding coverage", ex);
            }
        }
        public async Task<List<AdCoverage>> GetAllCoverages(CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetItemsByType<AdCoverage>(AdItemType.Coverage);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all coverages", ex);
            }
        }
        public async Task<AdRam> AddRam(string name, Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var ram = new AdRam
                {
                    Name = name,
                    ModelId = modelId,
                    Type = AdItemType.Ram
                };

                return await AddItem(ram);
            }
            catch(Exception ex)
            {
                throw new Exception("Error while adding RAM", ex);
            }            
        }

        public async Task<List<AdRam>> GetAllRams(CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetItemsByType<AdRam>(AdItemType.Ram);
            }
            catch(Exception ex)
            {
                throw new Exception("Error while Geting all RAM", ex);
            }
        }

        public async Task<List<AdRam>> GetRamsByModelId(Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var all = await GetItemsByType<AdRam>(AdItemType.Ram);
                return all.Where(r => r.ModelId == modelId).ToList();
            }
            catch(Exception ex)
            {
                throw new Exception("Error while getting Ram", ex);
            }
        }

        public async Task<AdResolution> AddResolution(string name, Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var resolution = new AdResolution
                {
                    Name = name,
                    ModelId = modelId,
                    Type = AdItemType.Resolution
                };

                return await AddItem(resolution);
            }
            catch(Exception ex)
            {
                throw new Exception("Error while adding Resolution", ex);
            }
        }

        public async Task<List<AdResolution>> GetAllResolutions(CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetItemsByType<AdResolution>(AdItemType.Resolution);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all resolutions", ex);
            }
        }

        public async Task<List<AdResolution>> GetResolutionsByModelId(Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var all = await GetItemsByType<AdResolution>(AdItemType.Resolution);
                return all.Where(r => r.ModelId == modelId).ToList();
            }
            catch(Exception ex)
            {
                throw new Exception("Error while getting Resolution", ex);
            }
        }

        public async Task<AdSizeType> AddSizeType(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var sizeType = new AdSizeType
                {
                    Name = name,
                    Type = AdItemType.SizeType
                };

                return await AddItem(sizeType);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while adding size type", ex);
            }
        }

        public async Task<List<AdSizeType>> GetAllSizeTypes(CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetItemsByType<AdSizeType>(AdItemType.SizeType);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while fetching all size types", ex);
            }
        }

        public async Task<AdGender> AddGender(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var gender = new AdGender
                {
                    Name = name,
                    Type = AdItemType.Gender
                };

                return await AddItem(gender);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while adding gender", ex);
            }
        }

        public async Task<List<AdGender>> GetAllGenders(CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetItemsByType<AdGender>(AdItemType.Gender);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all genders", ex);
            }
        }

        public async Task<AdZone> AddZone(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var zone = new AdZone
                {
                    Name = name,
                    Type = AdItemType.Zone
                };

                return await AddItem(zone);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while adding zone", ex);
            }
        }

        public async Task<List<AdZone>> GetAllZones(CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetItemsByType<AdZone>(AdItemType.Zone);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting zones", ex);
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

        public async Task<bool> SaveSearchByIdAsync(SaveSearchRequestByIdDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Search request cannot be null.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Search name is required.", nameof(dto.Name));

            if (dto.SearchQuery == null)
                throw new ArgumentException("Search query details are required.", nameof(dto.SearchQuery));

            try
            {
                var key = $"search:{dto.UserId}";

                var existing = await _dapr.GetStateAsync<List<SavedSearchResponseDto>>(UnifiedStore, key)
                               ?? new List<SavedSearchResponseDto>();

                var newSearch = new SavedSearchResponseDto
                {
                    Id = Guid.NewGuid(),
                    UserId = dto.UserId,
                    Name = dto.Name,
                    CreatedAt = DateTime.UtcNow,
                    SearchQuery = dto.SearchQuery
                };

                existing.Insert(0, newSearch);

                if (existing.Count > 30)
                    existing = existing.Take(30).ToList();

                await _dapr.SaveStateAsync(UnifiedStore, key, existing);

                var confirm = await _dapr.GetStateAsync<List<SavedSearchResponseDto>>(UnifiedStore, key);
                if (confirm == null || !confirm.Any(x => x.Id == newSearch.Id))
                {
                    throw new InvalidOperationException("Failed to confirm that the search was saved.");
                }

                return true;
            }
            catch (DaprException dex)
            {
                Console.WriteLine($"Dapr error while saving search: {dex.Message}");
                throw new InvalidOperationException("Failed to save search due to Dapr error.", dex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while saving search: {ex.Message}");
                throw new InvalidOperationException("An unexpected error occurred while saving search.", ex);
            }
        }

        public async Task<List<SavedSearchResponseDto>> GetSearchesAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("UserId is required.");

                var key = $"search:{userId}";
                var result = await _dapr.GetStateAsync<List<SavedSearchResponseDto>>(UnifiedStore, key);

                return result ?? new List<SavedSearchResponseDto>();
            }
            catch (DaprException dex)
            {
                Console.WriteLine($"Dapr error: {dex.Message}");
                throw new InvalidOperationException("Failed to retrieve saved searches due to Dapr error.", dex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw new InvalidOperationException("An unexpected error occurred while retrieving saved searches.", ex);
            }
        }

        public Task<bool> SaveSearchAsync(SaveSearchRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
