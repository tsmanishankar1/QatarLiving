using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService
{
    public interface IClassifiedService
    {
        Task<Category> AddCategory(string categoryName, CancellationToken cancellationToken = default);
        Task<List<CategoriesDto>> GetAllCategories(CancellationToken cancellationToken = default);
        Task<Category> AddSubCategory(string name, Guid categoryId, CancellationToken cancellationToken = default);
        Task<List<CategoriesDto>> GetAllSubCategories(CancellationToken cancellationToken = default);
        Task<CategoryDto?> GetCategoryWithSubCategories(Guid categoryId, CancellationToken cancellationToken = default);
        Task<Category> AddBrand(string name, Guid subCategoryId, CancellationToken cancellationToken = default);
        Task<List<CategoriesDto>> GetAllBrands(CancellationToken cancellationToken = default);
        Task<SubCategoryWithBrandsDto> GetSubCategoryWithBrands(Guid subCategoryId, CancellationToken cancellationToken = default);
        Task<Category> AddModel(string name, Guid brandId, CancellationToken cancellationToken = default);
        Task<List<CategoriesDto>> GetAllModels(CancellationToken cancellationToken = default);
        Task<BrandWithModelsDto> GetBrandWithModels(Guid brandId, CancellationToken cancellationToken = default);
        Task<Category> AddCondition(string name, CancellationToken cancellationToken = default);
        Task<List<CategoriesDto>> GetAllConditions(CancellationToken cancellationToken = default);
        Task<Category> AddColor(string name, CancellationToken cancellationToken = default);
        Task<List<CategoriesDto>> GetAllColors(CancellationToken cancellationToken = default);
        Task<Category> AddCapacity(string name, CancellationToken cancellationToken = default);
        Task<List<CategoriesDto>> GetAllCapacities(CancellationToken cancellationToken = default);
        Task<Category> AddProcessor(string name, Guid modelId, CancellationToken cancellationToken = default);
        Task<List<CategoriesDto>> GetAllProcessors(CancellationToken cancellationToken = default);
        Task<ModelWithProcessorsDto?> GetModelWithProcessors(Guid modelId, CancellationToken cancellationToken = default);
        Task<Category> AddCoverage(string name, CancellationToken cancellationToken = default);
        Task<List<CategoriesDto>> GetAllCoverages(CancellationToken cancellationToken = default);
        Task<Category> AddRam(string name, Guid modelId, CancellationToken cancellationToken = default);
        Task<List<CategoriesDto>> GetAllRams(CancellationToken cancellationToken = default);
        Task<ModelWithRamDto?> GetModelWithRam(Guid modelId, CancellationToken cancellationToken = default);
        Task<Category> AddResolution(string name, Guid modelId, CancellationToken cancellationToken = default);
        Task<List<CategoriesDto>> GetAllResolutions(CancellationToken cancellationToken = default);
        Task<ModelWithResolutionsDto?> GetModelWithResolutions(Guid modelId, CancellationToken cancellationToken = default);
        Task<Category> AddSizeType(string name, CancellationToken cancellationToken = default);
        Task<List<CategoriesDto>> GetAllSizeTypes(CancellationToken cancellationToken = default);
        Task<Category> AddZone(string name, CancellationToken cancellationToken = default);
        Task<List<CategoriesDto>> GetAllZones(CancellationToken cancellationToken = default);
        Task<NestedCategoryDto> GetCategoryHierarchy(Guid categoryId, CancellationToken cancellationToken = default);
        Task<bool> SaveSearch(SaveSearchRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> SaveSearchById(SaveSearchRequestByIdDto dto, CancellationToken cancellationToken = default);
        Task<List<SavedSearchResponseDto>> GetSearches(string userId, CancellationToken cancellationToken = default);
        Task<ItemAdsAndDashboardResponse> GetUserItemsAdsWithDashboard(Guid userId, CancellationToken cancellationToken = default);
        Task<PrelovedAdsAndDashboardResponse> GetUserPrelovedAdsAndDashboard(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> DeleteCategoryHierarchy(Guid categoryId, CancellationToken cancellationToken = default);
        Task<AdCreatedResponseDto> CreateClassifiedItemsAd(ClassifiedItems dto, CancellationToken cancellationToken = default);
        Task<AdCreatedResponseDto> CreateClassifiedPrelovedAd(ClassifiedPreloved dto, CancellationToken cancellationToken = default);
        Task<AdCreatedResponseDto> CreateClassifiedCollectiblesAd(ClassifiedCollectibles dto, CancellationToken cancellationToken = default);
        Task<CollectiblesResponse> GetCollectibles(string userId, CancellationToken cancellationToken = default);
        Task<AdCreatedResponseDto> CreateClassifiedDealsAd(ClassifiedDeals dto, CancellationToken cancellationToken = default);
        Task<DeleteAdResponseDto> DeleteClassifiedItemsAd(Guid adId, CancellationToken cancellationToken = default);
        Task<DeleteAdResponseDto> DeleteClassifiedPrelovedAd(Guid adId, CancellationToken cancellationToken = default);
        Task<DeleteAdResponseDto> DeleteClassifiedCollectiblesAd(Guid adId, CancellationToken cancellationToken = default);
        Task<DeleteAdResponseDto> DeleteClassifiedDealsAd(Guid adId, CancellationToken cancellationToken = default);
        Task<ItemAdListDto> GetUserItemsAd(Guid userId, CancellationToken cancellationToken = default);
        Task<PrelovedAdListDto> GetUserPrelovedAds(Guid userId, CancellationToken cancellationToken = default);
        Task<DealsAdListDto> GetUserDealsAds(Guid userId, CancellationToken cancellationToken = default);
        Task<CollectiblesAdListDto> GetUserCollectiblesAds(Guid userId, CancellationToken cancellationToken = default);
    }
}