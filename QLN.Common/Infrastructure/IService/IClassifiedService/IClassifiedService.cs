using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.BannerService
{
    public interface IClassifiedService
    {
        Task<IEnumerable<ClassifiedIndexDto>> Search(ClassifiedSearchRequest request);
        Task<ClassifiedIndexDto?> GetById(string id);
        Task<string> Upload(ClassifiedIndexDto document);
        Task<ClassifiedLandingPageResponse> GetLandingPage();       
        Task<AdCategory> AddCategory(AdCategory adCategory, CancellationToken cancellationToken = default);
        Task<List<AdCategory>> GetAllCategories(CancellationToken cancellationToken = default);
        Task<AdSubCategory> AddSubCategory(AdSubCategory subCategory, CancellationToken cancellationToken = default);
        Task<List<AdSubCategory>> GetAllSubCategories(CancellationToken cancellationToken = default);
        Task<List<AdSubCategory>> GetSubCategoriesByCategoryId(Guid categoryId, CancellationToken cancellationToken = default);
        Task<AdBrand> AddBrand(AdBrand brand);
        Task<List<AdBrand>> GetAllBrands();
        Task<List<AdBrand>> GetBrandsBySubCategoryId(Guid subCategoryId);
        Task<AdModel> AddModel(AdModel model, CancellationToken cancellationToken = default);
        Task<List<AdModel>> GetAllModels(CancellationToken cancellationToken = default);
        Task<List<AdModel>> GetModelsByBrandId(Guid brandId, CancellationToken cancellationToken = default);
        Task<AdCondition> AddCondition(AdCondition condition, CancellationToken cancellationToken = default);
        Task<List<AdCondition>> GetAllConditions(CancellationToken cancellationToken = default);
        Task<AdColor> AddColor(AdColor color, CancellationToken cancellationToken = default);
        Task<List<AdColor>> GetAllColors(CancellationToken cancellationToken = default);
        Task<AdCapacity> AddCapacity(AdCapacity capacity, CancellationToken cancellationToken = default);
        Task<List<AdCapacity>> GetAllCapacities(CancellationToken cancellationToken = default);
        Task<AdProcessor> AddProcessor(AdProcessor processor, CancellationToken cancellationToken = default);
        Task<List<AdProcessor>> GetAllProcessors(CancellationToken cancellationToken = default);
        Task<List<AdProcessor>> GetProcessorsByModelId(Guid modelId, CancellationToken cancellationToken = default);
        Task<AdCoverage> AddCoverage(AdCoverage coverage, CancellationToken cancellationToken = default);
        Task<List<AdCoverage>> GetAllCoverages(CancellationToken cancellationToken = default);
        Task<AdRam> AddRam(AdRam ram, CancellationToken cancellationToken = default);
        Task<List<AdRam>> GetAllRams(CancellationToken cancellationToken = default);
        Task<List<AdRam>> GetRamsByModelId(Guid modelId, CancellationToken cancellationToken = default);
        Task<AdResolution> AddResolution(AdResolution resolution, CancellationToken cancellationToken = default);
        Task<List<AdResolution>> GetAllResolutions(CancellationToken cancellationToken = default);
        Task<List<AdResolution>> GetResolutionsByModelId(Guid modelId, CancellationToken cancellationToken = default);
        Task<AdSizeType> AddSizeType(AdSizeType sizeType, CancellationToken cancellationToken = default);
        Task<List<AdSizeType>> GetAllSizeTypes(CancellationToken cancellationToken = default);
        Task<AdGender> AddGender(AdGender gender, CancellationToken cancellationToken = default);
        Task<List<AdGender>> GetAllGenders(CancellationToken cancellationToken = default);
        Task<AdZone> AddZone(AdZone zone, CancellationToken cancellationToken = default);
        Task<List<AdZone>> GetAllZones(CancellationToken cancellationToken = default);
        Task<string> CreateAd(AdInformation ad, string userId, CancellationToken token = default);
        Task<List<AdResponse>> GetUserAds(string userId, bool? isPublished, CancellationToken token = default);

    }
}