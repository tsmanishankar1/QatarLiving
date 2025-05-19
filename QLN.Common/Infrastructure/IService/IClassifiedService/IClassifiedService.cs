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
        Task<IEnumerable<ClassifiedIndexDto>> Search(CommonSearchRequest request);
        Task<ClassifiedIndexDto?> GetById(string id);
        Task<string> Upload(ClassifiedIndexDto document);
        Task<ClassifiedLandingPageResponse> GetLandingPage();
        Task<Adcateg> AddCategory(string categoryName, CancellationToken cancellationToken = default);
        Task<List<Adcateg>> GetAllCategories(CancellationToken cancellationToken = default);

        Task<AdSubCategory> AddSubCategory(string name, Guid categoryId, CancellationToken cancellationToken = default);
        Task<List<AdSubCategory>> GetAllSubCategories(CancellationToken cancellationToken = default);
        Task<List<AdSubCategory>> GetSubCategoriesByCategoryId(Guid categoryId);
        Task<AdBrand> AddBrand(string name, Guid subCategoryId, CancellationToken cancellationToken = default);
        Task<List<AdBrand>> GetAllBrands(CancellationToken cancellationToken = default);
        Task<List<AdBrand>> GetBrandsBySubCategoryId(Guid subCategoryId, CancellationToken cancellationToken = default);
        Task<AdModel> AddModel(string name, Guid brandId, CancellationToken cancellationToken = default);
        Task<List<AdModel>> GetAllModels(CancellationToken cancellationToken = default);
        Task<List<AdModel>> GetModelsByBrandId(Guid brandId, CancellationToken cancellationToken = default);
        Task<AdCondition> AddCondition(string name, CancellationToken cancellationToken = default);
        Task<List<AdCondition>> GetAllConditions(CancellationToken cancellationToken = default);
        Task<AdColor> AddColor(string name, CancellationToken cancellationToken = default);
        Task<List<AdColor>> GetAllColors(CancellationToken cancellationToken = default);
        Task<AdCapacity> AddCapacity(string name, CancellationToken cancellationToken = default);
        Task<List<AdCapacity>> GetAllCapacities(CancellationToken cancellationToken = default);
        Task<AdProcessor> AddProcessor(string name, Guid modelId, CancellationToken cancellationToken = default);
        Task<List<AdProcessor>> GetAllProcessors(CancellationToken cancellationToken = default);
        Task<List<AdProcessor>> GetProcessorsByModelId(Guid modelId, CancellationToken cancellationToken = default);
        Task<AdCoverage> AddCoverage(string name, CancellationToken cancellationToken = default);
        Task<List<AdCoverage>> GetAllCoverages(CancellationToken cancellationToken = default);
        Task<AdRam> AddRam(string name, Guid modelId, CancellationToken cancellationToken = default);
        Task<List<AdRam>> GetAllRams(CancellationToken cancellationToken = default);
        Task<List<AdRam>> GetRamsByModelId(Guid modelId, CancellationToken cancellationToken = default);
        Task<AdResolution> AddResolution(string name, Guid modelId, CancellationToken cancellationToken = default);
        Task<List<AdResolution>> GetAllResolutions(CancellationToken cancellationToken = default);
        Task<List<AdResolution>> GetResolutionsByModelId(Guid modelId, CancellationToken cancellationToken = default);
        Task<AdSizeType> AddSizeType(string name, CancellationToken cancellationToken = default);
        Task<List<AdSizeType>> GetAllSizeTypes(CancellationToken cancellationToken = default);
        Task<AdGender> AddGender(string name, CancellationToken cancellationToken = default);
        Task<List<AdGender>> GetAllGenders(CancellationToken cancellationToken = default);
        Task<AdZone> AddZone(string name, CancellationToken cancellationToken = default);
        Task<List<AdZone>> GetAllZones(CancellationToken cancellationToken = default);
        //Task<string> CreateAd(AdInformation ad, string userId, CancellationToken token = default);
        //Task<List<AdResponse>> GetUserAds(string userId, bool? isPublished, CancellationToken token = default);

    }
}