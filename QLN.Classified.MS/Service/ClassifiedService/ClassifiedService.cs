using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.BannerService;
using QLN.Common.Infrastructure.Model;

namespace QLN.Classified.MS.Service
{
    public class ClassifiedService : IClassifiedService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DaprClient _dapr;
        private const string SearchAppId = "qln-search-ms";
        private readonly IBannerService _bannerService;
        private const string AdStore = "adcategorystore";
        private const string IndexKey = "ad-category-index";
        private const string SubCategoryStore = "adsubcategorystore";
        private const string SubCategoryIndexKey = "ad-subcategory-index";
        private const string BrandStore = "adbrandstore";
        private const string BrandIndexKey = "ad-brand-index";
        private const string ModelStore = "admodelstore";
        private const string ModelIndexKey = "ad-model-index";
        private const string ConditionStore = "adconditionstore";
        private const string ConditionIndexKey = "ad-condition-index";
        private const string ColorStore = "adcolorstore";
        private const string ColorIndexKey = "ad-color-index";
        private const string CapacityStore = "adcapacitystore";
        private const string CapacityIndexKey = "ad-capacity-index";
        private const string ProcessorStore = "adprocessorstore";
        private const string ProcessorIndexKey = "ad-processor-index";
        private const string CoverageStore = "adcoveragestore";
        private const string CoverageIndexKey = "ad-coverage-index";
        private const string RamStore = "adramstore";
        private const string RamIndexKey = "ad-ram-index";
        private const string ResolutionStore = "adresolutionstore";
        private const string ResolutionIndexKey = "ad-resolution-index";
        private const string SizeTypeStore = "adsizetypestore";
        private const string SizeTypeIndexKey = "ad-size-type-index";
        private const string GenderStore = "adgenderstore";
        private const string GenderIndexKey = "ad-gender-index";
        private const string ZoneStore = "adzonestore";
        private const string ZoneIndexKey = "ad-zone-index";        
        private const string verticalAd = "adstore";
        private const string AdIndexKey = "ad-index";
        private readonly IWebHostEnvironment _env;
        
        public ClassifiedService(DaprClient dapr, IBannerService bannerService, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _bannerService = bannerService ?? throw new ArgumentNullException(nameof(bannerService));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _userManager = userManager;
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

        public async Task<AdCategory> AddCategory(AdCategory adCategory, CancellationToken cancellationToken = default)
        {
            try
            {
                var existingKeys = await _dapr.GetStateAsync<List<string>>(AdStore, IndexKey) ?? new();

                var id = Guid.NewGuid();
                adCategory.Id = id;

                var key = $"category-{id}";

                await _dapr.SaveStateAsync(AdStore, key, adCategory);

                existingKeys.Add(key);

                await _dapr.SaveStateAsync(AdStore, IndexKey, existingKeys);

                return adCategory;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while adding category", ex);
            }
        }

        public async Task<List<AdCategory>> GetAllCategories(CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(AdStore, IndexKey) ?? new();
                var result = new List<AdCategory>();

                foreach (var key in keys)
                {
                    var category = await _dapr.GetStateAsync<AdCategory>(AdStore, key);
                    if (category != null)
                    {
                        result.Add(category);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all categories", ex);
            }
        }

        public async Task<AdSubCategory> AddSubCategory(AdSubCategory subCategory, CancellationToken cancellationToken = default)
        {
            try
            {
                var id = Guid.NewGuid();
                subCategory.Id = id;
                var key = $"subcategory-{id}";

                var existingKeys = await _dapr.GetStateAsync<List<string>>(SubCategoryStore, SubCategoryIndexKey) ?? new();
                await _dapr.SaveStateAsync(SubCategoryStore, key, subCategory);

                existingKeys.Add(key);
                await _dapr.SaveStateAsync(SubCategoryStore, SubCategoryIndexKey, existingKeys);

                return subCategory;
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
                var keys = await _dapr.GetStateAsync<List<string>>(SubCategoryStore, SubCategoryIndexKey) ?? new();
                var result = new List<AdSubCategory>();

                foreach (var key in keys)
                {
                    var sub = await _dapr.GetStateAsync<AdSubCategory>(SubCategoryStore, key);
                    if (sub != null)
                        result.Add(sub);
                }

                return result;

            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all subcategories", ex);
            }
        }

        public async Task<List<AdSubCategory>> GetSubCategoriesByCategoryId(Guid categoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                var all = await GetAllSubCategories(cancellationToken);
                return all.Where(s => s.CategoryId == categoryId).ToList();
            }
            catch(Exception ex)
            {
                throw new Exception("Error while getting subcategories by category id", ex);
            }
        }

        public async Task<AdBrand> AddBrand(AdBrand brand)
        {
            try
            {
                var id = Guid.NewGuid();
                brand.Id = id;
                var key = $"brand-{id}";
                var index = await _dapr.GetStateAsync<List<string>>(BrandStore, BrandIndexKey) ?? new();
                await _dapr.SaveStateAsync(BrandStore, key, brand);
                index.Add(key);
                await _dapr.SaveStateAsync(BrandStore, BrandIndexKey, index);

                return brand;
            }
            catch(Exception ex)
            {
                throw new Exception("Error while adding brand", ex);
            }
        }

        public async Task<List<AdBrand>> GetAllBrands()
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(BrandStore, BrandIndexKey) ?? new();
                var result = new List<AdBrand>();
                foreach (var key in keys)
                {
                    var brand = await _dapr.GetStateAsync<AdBrand>(BrandStore, key);
                    if (brand != null)
                        result.Add(brand);
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all brands", ex);
            }
        }

        public async Task<List<AdBrand>> GetBrandsBySubCategoryId(Guid subCategoryId)
        {
            try
            {
                var data = await GetAllBrands();
                return data.Where(b => b.SubCategoryId == subCategoryId).ToList();
            }
            catch(Exception ex)
            {
                throw new Exception("Error while getting brands by subcategory id", ex);
            }
        }

        public async Task<AdModel> AddModel(AdModel model, CancellationToken cancellationToken = default)
        {
            try
            {
                var id = Guid.NewGuid();
                model.Id = id;
                var key = $"model-{id}";

                var index = await _dapr.GetStateAsync<List<string>>(ModelStore, ModelIndexKey) ?? new();
                await _dapr.SaveStateAsync(ModelStore, key, model);

                index.Add(key);
                await _dapr.SaveStateAsync(ModelStore, ModelIndexKey, index);

                return model;
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
                var keys = await _dapr.GetStateAsync<List<string>>(ModelStore, ModelIndexKey) ?? new();
                var result = new List<AdModel>();

                foreach (var key in keys)
                {
                    var model = await _dapr.GetStateAsync<AdModel>(ModelStore, key);
                    if (model != null)
                        result.Add(model);
                }

                return result;
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
                var all = await GetAllModels(cancellationToken);
                return all.Where(m => m.BrandId == brandId).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting models by brand ID", ex);
            }
        }

        public async Task<AdCondition> AddCondition(AdCondition condition, CancellationToken cancellationToken = default)
        {
            try
            {
                var id = Guid.NewGuid();
                condition.Id = id;
                var key = $"condition-{id}";

                var index = await _dapr.GetStateAsync<List<string>>(ConditionStore, ConditionIndexKey) ?? new();
                await _dapr.SaveStateAsync(ConditionStore, key, condition);

                index.Add(key);
                await _dapr.SaveStateAsync(ConditionStore, ConditionIndexKey, index);

                return condition;
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
                var keys = await _dapr.GetStateAsync<List<string>>(ConditionStore, ConditionIndexKey) ?? new();
                var result = new List<AdCondition>();

                foreach (var key in keys)
                {
                    var condition = await _dapr.GetStateAsync<AdCondition>(ConditionStore, key);
                    if (condition != null)
                        result.Add(condition);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting conditions", ex);
            }
        }

        public async Task<AdColor> AddColor(AdColor color, CancellationToken cancellationToken = default)
        {
            try
            {
                var id = Guid.NewGuid();
                color.Id = id;
                var key = $"color-{id}";

                var index = await _dapr.GetStateAsync<List<string>>(ColorStore, ColorIndexKey) ?? new();
                await _dapr.SaveStateAsync(ColorStore, key, color);

                index.Add(key);
                await _dapr.SaveStateAsync(ColorStore, ColorIndexKey, index);

                return color;
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
                var keys = await _dapr.GetStateAsync<List<string>>(ColorStore, ColorIndexKey) ?? new();
                var result = new List<AdColor>();

                foreach (var key in keys)
                {
                    var color = await _dapr.GetStateAsync<AdColor>(ColorStore, key);
                    if (color != null)
                        result.Add(color);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all colors", ex);
            }
        }

        public async Task<AdCapacity> AddCapacity(AdCapacity capacity, CancellationToken cancellationToken = default)
        {
            try
            {
                var id = Guid.NewGuid();
                capacity.Id = id;
                var key = $"capacity-{id}";

                var index = await _dapr.GetStateAsync<List<string>>(CapacityStore, CapacityIndexKey) ?? new();
                await _dapr.SaveStateAsync(CapacityStore, key, capacity);

                index.Add(key);
                await _dapr.SaveStateAsync(CapacityStore, CapacityIndexKey, index);

                return capacity;
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
                var keys = await _dapr.GetStateAsync<List<string>>(CapacityStore, CapacityIndexKey) ?? new();
                var result = new List<AdCapacity>();

                foreach (var key in keys)
                {
                    var item = await _dapr.GetStateAsync<AdCapacity>(CapacityStore, key);
                    if (item != null)
                        result.Add(item);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all capacities", ex);
            }
        }

        public async Task<AdProcessor> AddProcessor(AdProcessor processor, CancellationToken cancellationToken = default)
        {
            try
            {
                processor.Id = Guid.NewGuid();
                var key = $"processor-{processor.Id}";

                var index = await _dapr.GetStateAsync<List<string>>(ProcessorStore, ProcessorIndexKey) ?? new();
                await _dapr.SaveStateAsync(ProcessorStore, key, processor);

                index.Add(key);
                await _dapr.SaveStateAsync(ProcessorStore, ProcessorIndexKey, index);

                return processor;
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
                var keys = await _dapr.GetStateAsync<List<string>>(ProcessorStore, ProcessorIndexKey) ?? new();
                var result = new List<AdProcessor>();

                foreach (var key in keys)
                {
                    var item = await _dapr.GetStateAsync<AdProcessor>(ProcessorStore, key);
                    if (item != null)
                        result.Add(item);
                }

                return result;
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
                var all = await GetAllProcessors(cancellationToken);
                return all.Where(p => p.ModelId == modelId).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting processors by model ID", ex);
            }
        }

        public async Task<AdCoverage> AddCoverage(AdCoverage coverage, CancellationToken cancellationToken = default)
        {
            try
            {
                coverage.Id = Guid.NewGuid();
                var key = $"coverage-{coverage.Id}";

                var index = await _dapr.GetStateAsync<List<string>>(CoverageStore, CoverageIndexKey) ?? new();
                await _dapr.SaveStateAsync(CoverageStore, key, coverage);

                index.Add(key);
                await _dapr.SaveStateAsync(CoverageStore, CoverageIndexKey, index);

                return coverage;
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
                var keys = await _dapr.GetStateAsync<List<string>>(CoverageStore, CoverageIndexKey) ?? new();
                var result = new List<AdCoverage>();

                foreach (var key in keys)
                {
                    var item = await _dapr.GetStateAsync<AdCoverage>(CoverageStore, key);
                    if (item != null)
                        result.Add(item);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all coverages", ex);
            }
        }
        public async Task<AdRam> AddRam(AdRam ram, CancellationToken cancellationToken = default)
        {
            try
            {
                ram.Id = Guid.NewGuid();
                var key = $"ram-{ram.Id}";

                var index = await _dapr.GetStateAsync<List<string>>(RamStore, RamIndexKey) ?? new();
                await _dapr.SaveStateAsync(RamStore, key, ram);

                index.Add(key);
                await _dapr.SaveStateAsync(RamStore, RamIndexKey, index);

                return ram;
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
                var keys = await _dapr.GetStateAsync<List<string>>(RamStore, RamIndexKey) ?? new();
                var result = new List<AdRam>();
                foreach (var key in keys)
                {
                    var item = await _dapr.GetStateAsync<AdRam>(RamStore, key);
                    if (item != null) result.Add(item);
                }
                return result;
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
                var all = await GetAllRams(cancellationToken);
                return all.Where(x => x.ModelId == modelId).ToList();
            }
            catch(Exception ex)
            {
                throw new Exception("Error while getting Ram", ex);
            }
        }

        public async Task<AdResolution> AddResolution(AdResolution resolution, CancellationToken cancellationToken = default)
        {
            try
            {
                resolution.Id = Guid.NewGuid();
                var key = $"resolution-{resolution.Id}";

                var index = await _dapr.GetStateAsync<List<string>>(ResolutionStore, ResolutionIndexKey) ?? new();
                await _dapr.SaveStateAsync(ResolutionStore, key, resolution);

                index.Add(key);
                await _dapr.SaveStateAsync(ResolutionStore, ResolutionIndexKey, index);

                return resolution;
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
                var keys = await _dapr.GetStateAsync<List<string>>(ResolutionStore, ResolutionIndexKey) ?? new();
                var result = new List<AdResolution>();
                foreach (var key in keys)
                {
                    var item = await _dapr.GetStateAsync<AdResolution>(ResolutionStore, key);
                    if (item != null) result.Add(item);
                }
                return result;
            }
            catch(Exception ex)
            {
                throw new Exception("Error while getting all Resolution", ex);
            }
        }

        public async Task<List<AdResolution>> GetResolutionsByModelId(Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var all = await GetAllResolutions(cancellationToken);
                return all.Where(x => x.ModelId == modelId).ToList();
            }
            catch(Exception ex)
            {
                throw new Exception("Error while getting Resolution", ex);
            }
        }

        public async Task<AdSizeType> AddSizeType(AdSizeType sizeType, CancellationToken cancellationToken = default)
        {
            try
            {
                sizeType.Id = Guid.NewGuid();
                var key = $"sizetype-{sizeType.Id}";

                var index = await _dapr.GetStateAsync<List<string>>(SizeTypeStore, SizeTypeIndexKey) ?? new();
                await _dapr.SaveStateAsync(SizeTypeStore, key, sizeType);

                index.Add(key);
                await _dapr.SaveStateAsync(SizeTypeStore, SizeTypeIndexKey, index);

                return sizeType;
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
                var keys = await _dapr.GetStateAsync<List<string>>(SizeTypeStore, SizeTypeIndexKey) ?? new();
                var result = new List<AdSizeType>();

                foreach (var key in keys)
                {
                    var item = await _dapr.GetStateAsync<AdSizeType>(SizeTypeStore, key);
                    if (item != null)
                        result.Add(item);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while fetching all size types", ex);
            }
        }
        public async Task<AdGender> AddGender(AdGender gender, CancellationToken cancellationToken = default)
        {
            try
            {
                gender.Id = Guid.NewGuid();
                var key = $"gender-{gender.Id}";

                var index = await _dapr.GetStateAsync<List<string>>(GenderStore, GenderIndexKey) ?? new();
                await _dapr.SaveStateAsync(GenderStore, key, gender);

                index.Add(key);
                await _dapr.SaveStateAsync(GenderStore, GenderIndexKey, index);

                return gender;
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
                var keys = await _dapr.GetStateAsync<List<string>>(GenderStore, GenderIndexKey) ?? new();
                var result = new List<AdGender>();

                foreach (var key in keys)
                {
                    var gender = await _dapr.GetStateAsync<AdGender>(GenderStore, key);
                    if (gender != null)
                        result.Add(gender);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all genders", ex);
            }
        }

        public async Task<AdZone> AddZone(AdZone zone, CancellationToken cancellationToken = default)
        {
            try
            {
                zone.Id = Guid.NewGuid();
                var key = $"zone-{zone.Id}";

                var index = await _dapr.GetStateAsync<List<string>>(ZoneStore, ZoneIndexKey) ?? new();
                await _dapr.SaveStateAsync(ZoneStore, key, zone);

                index.Add(key);
                await _dapr.SaveStateAsync(ZoneStore, ZoneIndexKey, index);

                return zone;
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
                var keys = await _dapr.GetStateAsync<List<string>>(ZoneStore, ZoneIndexKey) ?? new();
                var result = new List<AdZone>();

                foreach (var key in keys)
                {
                    var zone = await _dapr.GetStateAsync<AdZone>(ZoneStore, key);
                    if (zone != null)
                        result.Add(zone);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting zones", ex);
            }
        }

        public async Task<string> CreateAd(AdInformation ad, string verticalName, string userId, CancellationToken token = default)
        {
            try
            {
                var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id.ToString() == userId && u.IsActive, cancellationToken: token);

                if (user == null)
                    throw new UnauthorizedAccessException("User is not registered or inactive.");

                var adId = Guid.NewGuid();                

                var licenseFileName = $"{adId}_license{Path.GetExtension(ad.WarrantyCertificate.FileName)}";
                var licensePath = Path.Combine(_env.WebRootPath, "images", "licenses", licenseFileName);

                Directory.CreateDirectory(Path.GetDirectoryName(licensePath)!);
                using (var stream = new FileStream(licensePath, FileMode.Create))
                {
                    await ad.WarrantyCertificate.CopyToAsync(stream, token);
                }

                var photoFileName = $"{adId}_photo{Path.GetExtension(ad.UploadPhotos.FileName)}";
                var photoPath = Path.Combine(_env.WebRootPath, "images", "ads", photoFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(photoPath)!);
                using (var stream = new FileStream(photoPath, FileMode.Create))
                {
                    await ad.UploadPhotos.CopyToAsync(stream, token);
                }

                var model = new AdResponse
                {
                    Id = adId,
                    Title = ad.Title,
                    Description = ad.Description,
                    Category = ad.Category,
                    SubCategory = ad.SubCategory,
                    Brand = ad.Brand,
                    Model = ad.Model,
                    Condition = ad.Condition,
                    Price = ad.Price,
                    Color = ad.Color,
                    Capacity = ad.Capacity,
                    Processor = ad.Processor,
                    Coverage = ad.Coverage,
                    Ram = ad.Ram,
                    Resolution = ad.Resolution,
                    BatteryPercentage = ad.BatteryPercentage,
                    Size = ad.Size,
                    SizeType = ad.SizeType,
                    Gender = ad.Gender,
                    WarrantyCertificateUrl = $"/images/licenses/{licenseFileName}",
                    ImageUrl = $"/images/ads/{photoFileName}",
                    PhoneNumber = ad.PhoneNumber,
                    WhatsappNumber = ad.WhatsappNumber,
                    Zone = ad.zone,
                    StreetNumber = ad.streetNumber,
                    BuildingNumber = ad.buildingNumber,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsPublished = ad.Ispublished
                };

                string stateKey = $"ad-{adId}";
                await _dapr.SaveStateAsync("adstore", stateKey, model, cancellationToken: token);

                var keys = await _dapr.GetStateAsync<List<string>>("adstore", "ad-index", cancellationToken: token) ?? new();
                keys.Add(stateKey);
                await _dapr.SaveStateAsync("adstore", "ad-index", keys, cancellationToken: token);

                return stateKey;

            }
            catch (Exception ex)
            {
                throw new Exception("Error while Ad posting", ex);
            }

        }

        /// <summary>
        /// Builds the landing-page model by querying all items and grouping.
        /// </summary>
        public async Task<ClassifiedLandingPageResponse> GetLandingPageAsync(
            string vertical)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical cannot be null or empty.", nameof(vertical));            
            var banners = await _bannerService.GetAllBanners();            
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
                Banners = banners ?? Enumerable.Empty<Banner>(),
                FeaturedItems = featuredItems,
                FeaturedCategories = featuredCategories,
                FeaturedStores = featuredStores,
                CategoryAdCounts = categoryCounts
            };
        }
    }
}