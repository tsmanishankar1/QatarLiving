using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using Google.Api;
using Microsoft.Extensions.Hosting;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Utilities;

namespace QLN.Backend.API.Service.ClassifiedService
{
    public class ExternalClassifiedService : IClassifiedService
    {
        private const string SERVICE_APP_ID = ConstantValues.ClassifiedServiceApp;
        private const string Vertical = ConstantValues.ClassifiedsVertical;

        private readonly DaprClient _dapr;
        private readonly IEventlogger _log;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ExternalClassifiedService(DaprClient dapr, IEventlogger log, IHttpContextAccessor httpContextAccessor)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Category> AddCategory(string categoryName, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<string, Category>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/category",
                    categoryName,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<CategoriesDto>> GetAllCategories(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<CategoriesDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/categories",
                    cancellationToken
                    );

                return result ?? new List<CategoriesDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<Category> AddSubCategory(string name, Guid categoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new
                {
                    Name = name,
                    CategoryId = categoryId
                };

                return await _dapr.InvokeMethodAsync<object, Category>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/subcategory",
                    body,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }
        public async Task<List<CategoriesDto>> GetAllSubCategories(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<CategoriesDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/subcategory",
                    cancellationToken
                );

                return result ?? new List<CategoriesDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<CategoryDto?> GetCategoryWithSubCategories(Guid categoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<CategoryDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/subcategory/by-category/{categoryId}"                    
                );

                return result ?? new CategoryDto();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<Category> AddBrand(string name, Guid subCategoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new
                {
                    Name = name,
                    SubCategoryId = subCategoryId
                };

                return await _dapr.InvokeMethodAsync<object, Category>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/brand",
                    body, cancellationToken                    
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<CategoriesDto>> GetAllBrands(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<CategoriesDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/brand"
                );

                return result ?? new List<CategoriesDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<SubCategoryWithBrandsDto> GetSubCategoryWithBrands(Guid subCategoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<SubCategoryWithBrandsDto> (
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/brand/by-subcategory/{subCategoryId}"
                );

                return result ?? new SubCategoryWithBrandsDto();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<Category> AddModel(string name, Guid brandId, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name, BrandId = brandId };

                return await _dapr.InvokeMethodAsync<object, Category>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/model",
                    body,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }
        public async Task<List<CategoriesDto>> GetAllModels(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<CategoriesDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/model",
                    cancellationToken
                );

                return result ?? new List<CategoriesDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }
        public async Task<BrandWithModelsDto> GetBrandWithModels(Guid brandId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<BrandWithModelsDto> (
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/model/by-brand/{brandId}",
                    cancellationToken
                );

                return result ?? new BrandWithModelsDto();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<Category> AddCondition(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name };

                return await _dapr.InvokeMethodAsync<object, Category>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/condition",
                    body,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<CategoriesDto>> GetAllConditions(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<CategoriesDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/condition",
                    cancellationToken
                );

                return result ?? new List<CategoriesDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<Category> AddColor(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name };

                return await _dapr.InvokeMethodAsync<object, Category>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/color",
                    body,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<CategoriesDto>> GetAllColors(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<CategoriesDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/color",
                    cancellationToken
                );

                return result ?? new List<CategoriesDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<Category> AddCapacity(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name };

                return await _dapr.InvokeMethodAsync<object, Category>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/capacity",
                    body,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<CategoriesDto>> GetAllCapacities(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<CategoriesDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/capacity",
                    cancellationToken
                );

                return result ?? new List<CategoriesDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<Category> AddProcessor(string name, Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name, ModelId = modelId };

                return await _dapr.InvokeMethodAsync<object, Category>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/processor",
                    body,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<CategoriesDto>> GetAllProcessors(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<CategoriesDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/processor",
                    cancellationToken
                );

                return result ?? new List<CategoriesDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<ModelWithProcessorsDto?> GetModelWithProcessors(Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<ModelWithProcessorsDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/processor/by-model/{modelId}",
                    cancellationToken
                );

                return result ?? new ModelWithProcessorsDto();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<Category> AddCoverage(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name };

                return await _dapr.InvokeMethodAsync<object, Category>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/coverage",
                    body,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<CategoriesDto>> GetAllCoverages(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<CategoriesDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/coverage",
                    cancellationToken
                );

                return result ?? new List<CategoriesDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }
        public async Task<Category> AddRam(string name, Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name, ModelId = modelId };

                return await _dapr.InvokeMethodAsync<object, Category>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/ram",
                    body,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<CategoriesDto>> GetAllRams(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<CategoriesDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/ram",
                    cancellationToken
                );

                return result ?? new List<CategoriesDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<ModelWithRamDto?> GetModelWithRam(Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<ModelWithRamDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/ram/by-model/{modelId}",
                    cancellationToken
                );

                return result ?? new ModelWithRamDto();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<Category> AddResolution(string name, Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name, ModelId = modelId };

                return await _dapr.InvokeMethodAsync<object, Category>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/resolution",
                    body,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<CategoriesDto>> GetAllResolutions(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<CategoriesDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/resolution",
                    cancellationToken
                );

                return result ?? new List<CategoriesDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<ModelWithResolutionsDto?> GetModelWithResolutions(Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<ModelWithResolutionsDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/resolution/by-model/{modelId}",
                    cancellationToken
                );

                return result ?? new ModelWithResolutionsDto();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<Category> AddSizeType(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name };

                return await _dapr.InvokeMethodAsync<object, Category>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/size-type",
                    body,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<CategoriesDto>> GetAllSizeTypes(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<CategoriesDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/size-type",
                    cancellationToken
                );

                return result ?? new List<CategoriesDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }
      
        public async Task<Category> AddZone(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name };

                return await _dapr.InvokeMethodAsync<object, Category>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/zone",
                    body,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<CategoriesDto>> GetAllZones(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<CategoriesDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/zone",
                    cancellationToken
                );

                return result ?? new List<CategoriesDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }   


        public async Task<CategoryHierarchyDto> GetCategoryHierarchy(Guid categoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (categoryId == Guid.Empty)
                    throw new ArgumentException("Invalid category ID", nameof(categoryId));

                var result = await _dapr.InvokeMethodAsync<CategoryHierarchyDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/category-hierarchy/{categoryId}",
                    cancellationToken
                );

                return result ?? throw new KeyNotFoundException($"No hierarchy found for category ID: {categoryId}");
            }
            catch(Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<ItemAdsAndDashboardResponse> GetUserItemsAdsWithDashboard(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<ItemAdsAndDashboardResponse>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/itemsAd-dashboard-byId?userId={userId}",
                    cancellationToken
                );

                return result;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<PrelovedAdsAndDashboardResponse> GetUserPrelovedAdsAndDashboard(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<PrelovedAdsAndDashboardResponse>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/prelovedAd-dashboard-byId?userId={userId}",
                    cancellationToken
                );

                return result;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<bool> SaveSearch(SaveSearchRequestDto dto ,Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestDto = new SaveSearchRequestByIdDto
                {
                UserId = userId,
                Name = dto.Name,
                CreatedAt = dto.CreatedAt,
                SearchQuery = dto.SearchQuery
                };

                var result = await _dapr.InvokeMethodAsync<SaveSearchRequestByIdDto, string>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/search/by-category-id",
                    requestDto,
                    cancellationToken
                );

                return !string.IsNullOrWhiteSpace(result);
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<SavedSearchResponseDto>> GetSearches(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID is required", nameof(userId));

                var result = await _dapr.InvokeMethodAsync<List<SavedSearchResponseDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/search/save-by-id?userId={userId}",
                    cancellationToken
                );

                return result ?? new List<SavedSearchResponseDto>();
            }
            catch (DaprException dex)
            {
                _log.LogException(dex);
                throw;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public Task<bool> SaveSearchById(SaveSearchRequestByIdDto dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<CollectiblesResponse> GetCollectibles(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID is required", nameof(userId));

                // Optional: log the request start
                _log.LogException(new Exception($"Starting to fetch collectibles for userId: {userId}"));

                var result = await _dapr.InvokeMethodAsync<CollectiblesResponse>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/collectibles-by-id?userId={userId}",
                    cancellationToken
                );

                // Optional: log success
                _log.LogException(new Exception($"Successfully fetched collectibles for userId: {userId}"));

                return result ?? new CollectiblesResponse();
            }
            catch (DaprException dex)
            {
                _log.LogException(new Exception($"DaprException while fetching collectibles for userId: {userId}. Message: {dex.Message}", dex));
                if (dex.InnerException != null)
                {
                    _log.LogException(new Exception($"Inner exception: {dex.InnerException.Message}", dex.InnerException));
                }

                throw new InvalidOperationException("Failed to fetch collectibles due to Dapr error.", dex);
            }
            catch (Exception ex)
            {
                _log.LogException(new Exception($"Unexpected error while fetching collectibles for userId: {userId}. Message: {ex.Message}", ex));
                if (ex.InnerException != null)
                {
                    _log.LogException(new Exception($"Inner exception: {ex.InnerException.Message}", ex.InnerException));
                }

                throw new InvalidOperationException("An unexpected error occurred while fetching collectibles.", ex);
            }
        }


    }
}
