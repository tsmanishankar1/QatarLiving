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
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService;
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

        private const string UnifiedStore = "adstore";
        private const string UnifiedIndexKey = "ad-index";
        private readonly ILogger<ClassifiedService> _logger;
        private readonly string jsonPath = "collectables.json";
        public ClassifiedService(Dapr.Client.DaprClient dapr, ILogger<ClassifiedService> logger, IWebHostEnvironment env)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                var items = common?.ClassifiedsItems ?? Enumerable.Empty<ClassifiedIndexDto>();

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

        private async Task<Category> AddItem(Category item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item), "Category item cannot be null.");

            if (string.IsNullOrWhiteSpace(item.TypePrefix))
                throw new ArgumentException("TypePrefix is required for the category item.", nameof(item.TypePrefix));

            try
            {
                item.Id = Guid.NewGuid();
                var key = $"{item.TypePrefix.ToLowerInvariant()}-{item.Id}";

                var index = await _dapr.GetStateAsync<List<string>>(UnifiedStore, UnifiedIndexKey) ?? new();
                index.Add(key);

                await _dapr.SaveStateAsync(UnifiedStore, key, item);
                await _dapr.SaveStateAsync(UnifiedStore, UnifiedIndexKey, index);

                return item;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error while saving category item '{item?.Name}' to Dapr state store. KeyPrefix: {item?.TypePrefix}",
                    ex
                );
            }
        }


        private async Task<List<Category>> GetItemsByType(string typePrefix)
        {
            if (string.IsNullOrWhiteSpace(typePrefix))
                throw new ArgumentException("typePrefix cannot be null or empty.", nameof(typePrefix));

            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(UnifiedStore, UnifiedIndexKey) ?? new();
                var result = new List<Category>();

                foreach (var key in keys)
                {
                    if (!key.StartsWith($"{typePrefix.ToLowerInvariant()}-")) continue;

                    var item = await _dapr.GetStateAsync<Category>(UnifiedStore, key);

                    if (item == null) continue;

                    if (item.TypePrefix.Equals(typePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(item);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error while retrieving categories of type '{typePrefix}' from state store.", ex);
            }
        }

        private string GetTypePrefix(AdInformation type)
        {
            try
            {
                var prefix = type.ToString().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(prefix))
                    throw new InvalidOperationException("Converted typePrefix is empty.");

                return prefix;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert AdInformation enum value '{type}' to type prefix.", ex);
            }
        }

        public async Task<Category> AddCategory(string categoryName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                throw new ArgumentException("Category name cannot be null or empty.", nameof(categoryName));

            try
            {
                var category = new Category
                {
                    Name = categoryName.Trim(),
                    TypePrefix = GetTypePrefix(AdInformation.Category),
                    ParentId = Guid.Empty,
                    IsActive = true
                };

                return await AddItem(category);
            }
            catch (ArgumentException argEx)
            {
                throw;
            }
            catch (InvalidOperationException logicEx)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while adding a new category.", ex);
            }
        }

        public async Task<List<CategoriesDto>> GetAllCategories(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await GetItemsByType(GetTypePrefix(AdInformation.Category));

                if (items == null || items.Count == 0)
                    return new List<CategoriesDto>();

                return items
                    .Where(i => i != null)
                    .Select(i => new CategoriesDto
                    {
                        Id = i.Id,
                        Name = i.Name
                    })
                    .ToList();
            }
            catch (InvalidOperationException logicEx)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while retrieving all categories.", ex);
            }
        }

        public async Task<Category> AddSubCategory(string name, Guid categoryId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Subcategory name cannot be null or empty.", nameof(name));

            if (categoryId == Guid.Empty)
                throw new ArgumentException("Parent Category ID is required.", nameof(categoryId));

            try
            {
                var subcategory = new Category
                {
                    Name = name.Trim(),
                    ParentId = categoryId,
                    TypePrefix = GetTypePrefix(AdInformation.SubCategory),
                    IsActive = true
                };

                return await AddItem(subcategory);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while adding subcategory.", ex);
            }
        }

        public async Task<List<CategoriesDto>> GetAllSubCategories(CancellationToken cancellationToken = default)
        {
            try
            {
                var typePrefix = GetTypePrefix(AdInformation.SubCategory);
                var items = await GetItemsByType(typePrefix);

                if (items == null || items.Count == 0)
                    return new List<CategoriesDto>();

                return items
                    .Where(i => i != null)
                    .Select(i => new CategoriesDto
                    {
                        Id = i.Id,
                        Name = i.Name
                    })
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while retrieving subcategories.", ex);
            }
        }

        public async Task<CategoryDto?> GetCategoryWithSubCategories(Guid categoryId, CancellationToken cancellationToken = default)
        {
            if (categoryId == Guid.Empty)
                throw new ArgumentException("Invalid category ID.", nameof(categoryId));

            try
            {
                var categoryKeyPrefix = GetTypePrefix(AdInformation.Category);
                var subCategoryKeyPrefix = GetTypePrefix(AdInformation.SubCategory);

                var allKeys = await _dapr.GetStateAsync<List<string>>(UnifiedStore, UnifiedIndexKey) ?? new();

                Category? selectedCategory = null;
                var subcategories = new List<CategoriesDto>();

                foreach (var key in allKeys)
                {
                    if (!key.StartsWith(categoryKeyPrefix) && !key.StartsWith(subCategoryKeyPrefix))
                        continue;

                    var item = await _dapr.GetStateAsync<Category>(UnifiedStore, key);
                    if (item == null) continue;

                    if (item.TypePrefix == categoryKeyPrefix && item.Id == categoryId)
                    {
                        selectedCategory = item;
                    }
                    else if (item.TypePrefix == subCategoryKeyPrefix && item.ParentId == categoryId)
                    {
                        subcategories.Add(new CategoriesDto
                        {
                            Id = item.Id,
                            Name = item.Name
                        });
                    }
                }

                if (selectedCategory == null)
                    throw new KeyNotFoundException($"Category with ID '{categoryId}' not found.");

                return new CategoryDto
                {
                    Id = selectedCategory.Id,
                    Name = selectedCategory.Name,
                    SubCategories = subcategories
                };
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while loading category with its subcategories.", ex);
            }
        }

        public async Task<Category> AddBrand(string name, Guid subCategoryId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Brand name cannot be null or empty.", nameof(name));

            if (subCategoryId == Guid.Empty)
                throw new ArgumentException("SubCategoryId must be a valid non-empty GUID.", nameof(subCategoryId));

            try
            {
                var brand = new Category
                {
                    Name = name.Trim(),
                    ParentId = subCategoryId,
                    TypePrefix = GetTypePrefix(AdInformation.Brand),
                    IsActive = true
                };

                return await AddItem(brand);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while adding brand.", ex);
            }
        }

        public async Task<List<CategoriesDto>> GetAllBrands(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await GetItemsByType(GetTypePrefix(AdInformation.Brand));

                if (items == null || items.Count == 0)
                    return new List<CategoriesDto>();

                return items
                    .Where(i => i != null)
                    .Select(i => new CategoriesDto { Id = i.Id, Name = i.Name })
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while retrieving brand list.", ex);
            }
        }

        public async Task<SubCategoryWithBrandsDto> GetSubCategoryWithBrands(Guid subCategoryId, CancellationToken cancellationToken = default)
        {
            if (subCategoryId == Guid.Empty)
                throw new ArgumentException("SubCategoryId must be a valid non-empty GUID.", nameof(subCategoryId));

            try
            {
                var subCategoryKeyPrefix = GetTypePrefix(AdInformation.SubCategory);
                var brandKeyPrefix = GetTypePrefix(AdInformation.Brand);

                var allKeys = await _dapr.GetStateAsync<List<string>>(UnifiedStore, UnifiedIndexKey) ?? new();

                Category? selectedSubCategory = null;
                var brandList = new List<BrandDto>();

                foreach (var key in allKeys)
                {
                    if (!key.StartsWith(subCategoryKeyPrefix) && !key.StartsWith(brandKeyPrefix))
                        continue;

                    var item = await _dapr.GetStateAsync<Category>(UnifiedStore, key);
                    if (item == null) continue;

                    if (item.TypePrefix == subCategoryKeyPrefix && item.Id == subCategoryId)
                    {
                        selectedSubCategory = item;
                    }
                    else if (item.TypePrefix == brandKeyPrefix && item.ParentId == subCategoryId)
                    {
                        brandList.Add(new BrandDto
                        {
                            Id = item.Id,
                            Name = item.Name
                        });
                    }
                }

                if (selectedSubCategory == null)
                    throw new KeyNotFoundException($"Subcategory with ID '{subCategoryId}' was not found.");

                return new SubCategoryWithBrandsDto
                {
                    Id = selectedSubCategory.Id,
                    Name = selectedSubCategory.Name,
                    Brands = brandList
                };
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while loading subcategory with its brands.", ex);
            }
        }

        public async Task<Category> AddModel(string name, Guid brandId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Model name cannot be null or empty.", nameof(name));

            if (brandId == Guid.Empty)
                throw new ArgumentException("Brand ID must be a valid non-empty GUID.", nameof(brandId));

            try
            {
                var model = new Category
                {
                    Name = name.Trim(),
                    ParentId = brandId,
                    TypePrefix = GetTypePrefix(AdInformation.Model),
                    IsActive = true
                };

                return await AddItem(model);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while adding model.", ex);
            }
        }

        public async Task<List<CategoriesDto>> GetAllModels(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await GetItemsByType(GetTypePrefix(AdInformation.Model));

                if (items == null || items.Count == 0)
                    return new List<CategoriesDto>();

                return items
                    .Where(i => i != null)
                    .Select(i => new CategoriesDto
                    {
                        Id = i.Id,
                        Name = i.Name
                    })
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error while retrieving model list.", ex);
            }
        }

        public async Task<BrandWithModelsDto> GetBrandWithModels(Guid brandId, CancellationToken cancellationToken = default)
        {
            if (brandId == Guid.Empty)
                throw new ArgumentException("Brand ID must be a valid non-empty GUID.", nameof(brandId));

            try
            {
                var brandPrefix = GetTypePrefix(AdInformation.Brand);
                var modelPrefix = GetTypePrefix(AdInformation.Model);

                var allKeys = await _dapr.GetStateAsync<List<string>>(UnifiedStore, UnifiedIndexKey) ?? new();

                Category? selectedBrand = null;
                var models = new List<ModelDto>();

                foreach (var key in allKeys)
                {
                    if (!key.StartsWith(brandPrefix) && !key.StartsWith(modelPrefix))
                        continue;

                    var item = await _dapr.GetStateAsync<Category>(UnifiedStore, key);
                    if (item == null) continue;

                    if (item.TypePrefix == brandPrefix && item.Id == brandId)
                    {
                        selectedBrand = item;
                    }
                    else if (item.TypePrefix == modelPrefix && item.ParentId == brandId)
                    {
                        models.Add(new ModelDto
                        {
                            Id = item.Id,
                            Name = item.Name
                        });
                    }
                }

                if (selectedBrand == null)
                    throw new KeyNotFoundException($"Brand with ID '{brandId}' not found.");

                return new BrandWithModelsDto
                {
                    Id = selectedBrand.Id,
                    Name = selectedBrand.Name,
                    Models = models
                };
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while loading brand with its models.", ex);
            }
        }

        public async Task<Category> AddCondition(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Condition name cannot be null or empty.", nameof(name));

            try
            {
                var condition = new Category
                {
                    Name = name.Trim(),
                    TypePrefix = GetTypePrefix(AdInformation.Condition),
                    ParentId = Guid.Empty,
                    IsActive = true
                };

                return await AddItem(condition);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while adding condition.", ex);
            }
        }

        public async Task<List<CategoriesDto>> GetAllConditions(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await GetItemsByType(GetTypePrefix(AdInformation.Condition));

                if (items == null || items.Count == 0)
                    return new List<CategoriesDto>();

                return items
                    .Where(i => i != null)
                    .Select(i => new CategoriesDto { Id = i.Id, Name = i.Name })
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while retrieving conditions.", ex);
            }
        }

        public async Task<Category> AddColor(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Color name cannot be null or empty.", nameof(name));

            try
            {
                var color = new Category
                {
                    Name = name.Trim(),
                    TypePrefix = GetTypePrefix(AdInformation.Color),
                    ParentId = Guid.Empty,
                    IsActive = true
                };

                return await AddItem(color);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while adding color.", ex);
            }
        }

        public async Task<List<CategoriesDto>> GetAllColors(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await GetItemsByType(GetTypePrefix(AdInformation.Color));

                if (items == null || items.Count == 0)
                    return new List<CategoriesDto>();

                return items
                    .Where(i => i != null)
                    .Select(i => new CategoriesDto
                    {
                        Id = i.Id,
                        Name = i.Name
                    })
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while retrieving colors.", ex);
            }
        }

        public async Task<Category> AddCapacity(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Capacity name cannot be null or empty.", nameof(name));

            try
            {
                var capacity = new Category
                {
                    Name = name.Trim(),
                    TypePrefix = GetTypePrefix(AdInformation.Capacity),
                    ParentId = Guid.Empty,
                    IsActive = true
                };

                return await AddItem(capacity);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while adding capacity.", ex);
            }
        }

        public async Task<List<CategoriesDto>> GetAllCapacities(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await GetItemsByType(GetTypePrefix(AdInformation.Capacity));

                if (items == null || items.Count == 0)
                    return new List<CategoriesDto>();

                return items
                    .Where(i => i != null)
                    .Select(i => new CategoriesDto
                    {
                        Id = i.Id,
                        Name = i.Name
                    })
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while retrieving capacity list.", ex);
            }
        }

        public async Task<Category> AddProcessor(string name, Guid modelId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Processor name cannot be null or empty.", nameof(name));

            if (modelId == Guid.Empty)
                throw new ArgumentException("Model ID must be a valid non-empty GUID.", nameof(modelId));

            try
            {
                var processor = new Category
                {
                    Name = name.Trim(),
                    ParentId = modelId,
                    TypePrefix = GetTypePrefix(AdInformation.Processor),
                    IsActive = true
                };

                return await AddItem(processor);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while adding processor.", ex);
            }
        }

        public async Task<List<CategoriesDto>> GetAllProcessors(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await GetItemsByType(GetTypePrefix(AdInformation.Processor));

                if (items == null || items.Count == 0)
                    return new List<CategoriesDto>();

                return items
                    .Where(i => i != null)
                    .Select(i => new CategoriesDto
                    {
                        Id = i.Id,
                        Name = i.Name
                    })
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while retrieving processors.", ex);
            }
        }

        public async Task<ModelWithProcessorsDto?> GetModelWithProcessors(Guid modelId, CancellationToken cancellationToken = default)
        {
            if (modelId == Guid.Empty)
                throw new ArgumentException("Model ID must be a valid non-empty GUID.", nameof(modelId));

            try
            {
                var modelPrefix = GetTypePrefix(AdInformation.Model);
                var processorPrefix = GetTypePrefix(AdInformation.Processor);

                var keys = await _dapr.GetStateAsync<List<string>>(UnifiedStore, UnifiedIndexKey) ?? new();

                Category? selectedModel = null;
                var processors = new List<ProcessorDto>();

                foreach (var key in keys)
                {
                    if (!key.StartsWith(modelPrefix) && !key.StartsWith(processorPrefix))
                        continue;

                    var item = await _dapr.GetStateAsync<Category>(UnifiedStore, key);
                    if (item == null) continue;

                    if (item.TypePrefix == modelPrefix && item.Id == modelId)
                        selectedModel = item;

                    if (item.TypePrefix == processorPrefix && item.ParentId == modelId)
                    {
                        processors.Add(new ProcessorDto
                        {
                            Id = item.Id,
                            Name = item.Name
                        });
                    }
                }

                if (selectedModel == null)
                    throw new KeyNotFoundException($"Model with ID '{modelId}' not found.");

                return new ModelWithProcessorsDto
                {
                    Id = selectedModel.Id,
                    Name = selectedModel.Name,
                    Processors = processors
                };
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while loading model with processors.", ex);
            }
        }

        public async Task<Category> AddCoverage(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Coverage name cannot be null or empty.", nameof(name));

            try
            {
                var coverage = new Category
                {
                    Name = name.Trim(),
                    TypePrefix = GetTypePrefix(AdInformation.Coverage),
                    ParentId = Guid.Empty,
                    IsActive = true
                };

                return await AddItem(coverage);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while adding coverage.", ex);
            }
        }

        public async Task<List<CategoriesDto>> GetAllCoverages(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await GetItemsByType(GetTypePrefix(AdInformation.Coverage));

                if (items == null || items.Count == 0)
                    return new List<CategoriesDto>();

                return items
                    .Where(i => i != null)
                    .Select(i => new CategoriesDto
                    {
                        Id = i.Id,
                        Name = i.Name
                    })
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while retrieving coverage list.", ex);
            }
        }

        public async Task<Category> AddRam(string name, Guid modelId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("RAM name cannot be null or empty.", nameof(name));

            if (modelId == Guid.Empty)
                throw new ArgumentException("Model ID must be a valid non-empty GUID.", nameof(modelId));

            try
            {
                var ram = new Category
                {
                    Name = name.Trim(),
                    ParentId = modelId,
                    TypePrefix = GetTypePrefix(AdInformation.Ram),
                    IsActive = true
                };

                return await AddItem(ram);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while adding RAM.", ex);
            }
        }

        public async Task<List<CategoriesDto>> GetAllRams(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await GetItemsByType(GetTypePrefix(AdInformation.Ram));

                if (items == null || items.Count == 0)
                    return new List<CategoriesDto>();

                return items
                    .Where(i => i != null)
                    .Select(i => new CategoriesDto { Id = i.Id, Name = i.Name })
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while retrieving RAM options.", ex);
            }
        }

        public async Task<ModelWithRamDto?> GetModelWithRam(Guid modelId, CancellationToken cancellationToken = default)
        {
            if (modelId == Guid.Empty)
                throw new ArgumentException("Model ID must be a valid non-empty GUID.", nameof(modelId));

            try
            {
                var modelPrefix = GetTypePrefix(AdInformation.Model);
                var ramPrefix = GetTypePrefix(AdInformation.Ram);

                var allKeys = await _dapr.GetStateAsync<List<string>>(UnifiedStore, UnifiedIndexKey) ?? new();

                Category? selectedModel = null;
                var ramList = new List<CategoriesDto>();

                foreach (var key in allKeys)
                {
                    if (!key.StartsWith(modelPrefix) && !key.StartsWith(ramPrefix))
                        continue;

                    var item = await _dapr.GetStateAsync<Category>(UnifiedStore, key);
                    if (item == null) continue;

                    if (item.TypePrefix == modelPrefix && item.Id == modelId)
                        selectedModel = item;

                    if (item.TypePrefix == ramPrefix && item.ParentId == modelId)
                    {
                        ramList.Add(new CategoriesDto
                        {
                            Id = item.Id,
                            Name = item.Name
                        });
                    }
                }

                if (selectedModel == null)
                    throw new KeyNotFoundException($"Model with ID '{modelId}' not found.");

                return new ModelWithRamDto
                {
                    Id = selectedModel.Id,
                    Name = selectedModel.Name,
                    RamOptions = ramList
                };
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error while loading RAM linked to the selected model.", ex);
            }
        }

        public async Task<Category> AddResolution(string name, Guid modelId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Resolution name cannot be null or empty.", nameof(name));

            if (modelId == Guid.Empty)
                throw new ArgumentException("Model ID must be a valid non-empty GUID.", nameof(modelId));

            try
            {
                var resolution = new Category
                {
                    Name = name.Trim(),
                    TypePrefix = GetTypePrefix(AdInformation.Resolution),
                    ParentId = modelId,
                    IsActive = true
                };

                return await AddItem(resolution);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while adding resolution.", ex);
            }
        }

        public async Task<List<CategoriesDto>> GetAllResolutions(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await GetItemsByType(GetTypePrefix(AdInformation.Resolution));

                if (items == null || items.Count == 0)
                    return new List<CategoriesDto>();

                return items
                    .Where(i => i != null)
                    .Select(i => new CategoriesDto { Id = i.Id, Name = i.Name })
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while retrieving resolution list.", ex);
            }
        }

        public async Task<ModelWithResolutionsDto?> GetModelWithResolutions(Guid modelId, CancellationToken cancellationToken = default)
        {
            if (modelId == Guid.Empty)
                throw new ArgumentException("Model ID must be a valid non-empty GUID.", nameof(modelId));

            try
            {
                var modelPrefix = GetTypePrefix(AdInformation.Model);
                var resolutionPrefix = GetTypePrefix(AdInformation.Resolution);

                var allKeys = await _dapr.GetStateAsync<List<string>>(UnifiedStore, UnifiedIndexKey) ?? new();

                Category? selectedModel = null;
                var resolutions = new List<CategoriesDto>();

                foreach (var key in allKeys)
                {
                    if (!key.StartsWith(modelPrefix) && !key.StartsWith(resolutionPrefix))
                        continue;

                    var item = await _dapr.GetStateAsync<Category>(UnifiedStore, key);
                    if (item == null) continue;

                    if (item.TypePrefix == modelPrefix && item.Id == modelId)
                        selectedModel = item;

                    if (item.TypePrefix == resolutionPrefix && item.ParentId == modelId)
                    {
                        resolutions.Add(new CategoriesDto
                        {
                            Id = item.Id,
                            Name = item.Name
                        });
                    }
                }

                if (selectedModel == null)
                    throw new KeyNotFoundException($"Model with ID '{modelId}' not found.");

                return new ModelWithResolutionsDto
                {
                    Id = selectedModel.Id,
                    Name = selectedModel.Name,
                    Resolutions = resolutions
                };
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error while loading model with resolutions.", ex);
            }
        }

        public async Task<Category> AddSizeType(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Size type name cannot be null or empty.", nameof(name));

            try
            {
                var sizeType = new Category
                {
                    Name = name.Trim(),
                    TypePrefix = GetTypePrefix(AdInformation.SizeType),
                    ParentId = Guid.Empty,
                    IsActive = true
                };

                return await AddItem(sizeType);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while adding size type.", ex);
            }
        }

        public async Task<List<CategoriesDto>> GetAllSizeTypes(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await GetItemsByType(GetTypePrefix(AdInformation.SizeType));
                if (items == null || items.Count == 0)
                    return new List<CategoriesDto>();

                return items
                    .Where(i => i != null)
                    .Select(i => new CategoriesDto { Id = i.Id, Name = i.Name })
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while retrieving all size types.", ex);
            }
        }

        public async Task<Category> AddZone(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Zone name cannot be null or empty.", nameof(name));

            try
            {
                var zone = new Category
                {
                    Name = name.Trim(),
                    TypePrefix = GetTypePrefix(AdInformation.Zone),
                    ParentId = Guid.Empty,
                    IsActive = true
                };

                return await AddItem(zone);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while adding zone.", ex);
            }
        }


        public async Task<List<CategoriesDto>> GetAllZones(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await GetItemsByType(GetTypePrefix(AdInformation.Zone));
                if (items == null || items.Count == 0)
                    return new List<CategoriesDto>();

                return items
                    .Where(i => i != null)
                    .Select(i => new CategoriesDto { Id = i.Id, Name = i.Name })
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while retrieving all zones.", ex);
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

        public async Task<bool> SaveSearchById(SaveSearchRequestByIdDto dto, CancellationToken cancellationToken = default)
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

        public async Task<List<SavedSearchResponseDto>> GetSearches(string userId, CancellationToken cancellationToken = default)
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

        public Task<bool> SaveSearch(SaveSearchRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public async Task<CollectiblesResponse> GetCollectibles(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID is required", nameof(userId));

                var fullPath = Path.Combine(_env.ContentRootPath, jsonPath);

                if (!File.Exists(fullPath))
                    throw new FileNotFoundException("JSON data file not found", fullPath);

                var json = await File.ReadAllTextAsync(fullPath, cancellationToken);
                var allData = JsonSerializer.Deserialize<CollectiblesResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });


                if (allData == null)
                    return new CollectiblesResponse();

                var targetUserId = Guid.Parse(userId);

                if (allData.UserId != targetUserId)
                    return new CollectiblesResponse();

                return allData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading collectibles: {ex.Message}");
                throw;
            }
        }

    }
}
