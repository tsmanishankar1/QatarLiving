using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Dapr.Client;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.BannerService;
using QLN.Common.Infrastructure.Model;

namespace QLN.Backend.API.Service.ClassifiedService
{
    public class ExternalClassifiedService : IClassifiedService
    {
        private const string SERVICE_APP_ID = "qln-classified-ms";
        private readonly DaprClient _dapr;
        private readonly IEventlogger _log;

        public ExternalClassifiedService(DaprClient dapr, IEventlogger log)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task<IEnumerable<ClassifiedIndexDto>> SearchAsync(
            string vertical,
            ClassifiedSearchRequest request)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<
                    ClassifiedSearchRequest,
                    ClassifiedIndexDto[]>(
                        HttpMethod.Post,
                        SERVICE_APP_ID,
                        $"api/{vertical}/search",
                        request
                    );

                return result ?? Array.Empty<ClassifiedIndexDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<ClassifiedIndexDto?> GetByIdAsync(
            string vertical,
            string id)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<ClassifiedIndexDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{vertical}/{id}"
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<string> UploadAsync(
            string vertical,
            ClassifiedIndexDto document)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<
                    ClassifiedIndexDto,
                    string>(
                        HttpMethod.Post,
                        SERVICE_APP_ID,
                        $"api/{vertical}/upload",
                        document
                    );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<ClassifiedLandingPageResponse> GetLandingPageAsync(
            string vertical)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<ClassifiedLandingPageResponse>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{vertical}/landing"
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdCategory> AddCategory(AdCategory adCategory, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<AdCategory, AdCategory>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/category",
                    adCategory
                );
            }
            catch(Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdCategory>> GetAllCategories(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdCategory>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/categories",
                    cancellationToken
                    );

                return result ?? new List<AdCategory>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }
        public async Task<AdSubCategory> AddSubCategory(AdSubCategory subCategory, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<AdSubCategory, AdSubCategory>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/subcategory",
                    subCategory,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }
        public async Task<List<AdSubCategory>> GetAllSubCategories(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdSubCategory>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/subcategory",
                    cancellationToken
                );

                return result ?? new List<AdSubCategory>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdSubCategory>> GetSubCategoriesByCategoryId(Guid categoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdSubCategory>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/subcategory/by-category/{categoryId}",
                    cancellationToken
                );

                return result ?? new List<AdSubCategory>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdBrand> AddBrand(AdBrand brand)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<AdBrand, AdBrand>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/brand",
                    brand                    
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdBrand>> GetAllBrands()
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdBrand>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/brand"                    
                );

                return result ?? new List<AdBrand>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdBrand>> GetBrandsBySubCategoryId(Guid subCategoryId)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdBrand>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/brand/by-subcategory/{subCategoryId}"                    
                );

                return result ?? new List<AdBrand>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdModel> AddModel(AdModel model, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<AdModel, AdModel>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/model",
                    model,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }
        public async Task<List<AdModel>> GetAllModels(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdModel>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/model",
                    cancellationToken
                );

                return result ?? new List<AdModel>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }
        public async Task<List<AdModel>> GetModelsByBrandId(Guid brandId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdModel>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/model/by-brand/{brandId}",
                    cancellationToken
                );

                return result ?? new List<AdModel>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdCondition> AddCondition(AdCondition condition, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<AdCondition, AdCondition>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/condition",
                    condition,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdCondition>> GetAllConditions(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdCondition>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/condition",
                    cancellationToken
                );

                return result ?? new List<AdCondition>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdColor> AddColor(AdColor color, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<AdColor, AdColor>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/color",
                    color,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdColor>> GetAllColors(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdColor>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/color",
                    cancellationToken
                );

                return result ?? new List<AdColor>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdCapacity> AddCapacity(AdCapacity capacity, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<AdCapacity, AdCapacity>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/capacity",
                    capacity,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdCapacity>> GetAllCapacities(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdCapacity>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/capacity",
                    cancellationToken
                );

                return result ?? new List<AdCapacity>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdProcessor> AddProcessor(AdProcessor processor, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<AdProcessor, AdProcessor>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/processor",
                    processor,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdProcessor>> GetAllProcessors(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdProcessor>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/processor",
                    cancellationToken
                );

                return result ?? new List<AdProcessor>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdProcessor>> GetProcessorsByModelId(Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdProcessor>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/processor/by-model/{modelId}",
                    cancellationToken
                );

                return result ?? new List<AdProcessor>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdCoverage> AddCoverage(AdCoverage coverage, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<AdCoverage, AdCoverage>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/coverage",
                    coverage,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdCoverage>> GetAllCoverages(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdCoverage>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/coverage",
                    cancellationToken
                );

                return result ?? new List<AdCoverage>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }
        public async Task<AdRam> AddRam(AdRam ram, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<AdRam, AdRam>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/ram",
                    ram,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdRam>> GetAllRams(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdRam>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/ram",
                    cancellationToken
                );

                return result ?? new List<AdRam>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdRam>> GetRamsByModelId(Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdRam>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/ram/by-model/{modelId}",
                    cancellationToken
                );

                return result ?? new List<AdRam>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdResolution> AddResolution(AdResolution resolution, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<AdResolution, AdResolution>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/resolution",
                    resolution,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdResolution>> GetAllResolutions(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdResolution>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/resolution",
                    cancellationToken
                );

                return result ?? new List<AdResolution>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdResolution>> GetResolutionsByModelId(Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdResolution>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/resolution/by-model/{modelId}",
                    cancellationToken
                );

                return result ?? new List<AdResolution>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdSizeType> AddSizeType(AdSizeType sizeType, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<AdSizeType, AdSizeType>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/size-type",
                    sizeType,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdSizeType>> GetAllSizeTypes(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdSizeType>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/size-type",
                    cancellationToken
                );

                return result ?? new List<AdSizeType>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdGender> AddGender(AdGender gender, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<AdGender, AdGender>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/gender",
                    gender,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdGender>> GetAllGenders(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdGender>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/gender",
                    cancellationToken
                );

                return result ?? new List<AdGender>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdZone> AddZone(AdZone zone, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<AdZone, AdZone>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/zone",
                    zone,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<List<AdZone>> GetAllZones(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdZone>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/zone",
                    cancellationToken
                );

                return result ?? new List<AdZone>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<string> CreateAd(AdInformation ad, string verticalName, string userId, CancellationToken token = default)
        {
            try
            {
                var url = $"api/{verticalName}/ad";
                return await _dapr.InvokeMethodAsync<AdInformation, string>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    url,
                    ad,
                    cancellationToken: token);
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

    }
}
