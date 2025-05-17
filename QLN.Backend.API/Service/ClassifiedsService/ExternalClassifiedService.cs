using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.BannerService;
using QLN.Common.Infrastructure.Model;

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

        public async Task<IEnumerable<ClassifiedIndexDto>> Search(ClassifiedSearchRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            try
            {
                var result = await _dapr.InvokeMethodAsync<
                    ClassifiedSearchRequest,
                    ClassifiedIndexDto[]>(
                        HttpMethod.Post,
                        SERVICE_APP_ID,
                        $"/api/{Vertical}/search",
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

        public async Task<ClassifiedIndexDto?> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id is required", nameof(id));

            try
            {
                return await _dapr.InvokeMethodAsync<ClassifiedIndexDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"/api/{Vertical}/{id}"
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<string> Upload(ClassifiedIndexDto document)
        {
            if (document is null) throw new ArgumentNullException(nameof(document));

            /*            var req = new CommonIndexRequest
                        {
                            VerticalName = Vertical,
                            ClassifiedsItem = document
                        };*/

            try
            {
                return await _dapr.InvokeMethodAsync<ClassifiedIndexDto, string>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"/api/{Vertical}/upload",
                    document,
                    CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException(
                    $"Dapr invoke to '/api/{Vertical}/upload' failed: {ex.Message}",
                    ex
                );
            }
        }

        public async Task<ClassifiedLandingPageResponse> GetLandingPage()
        {
            try
            {
                return await _dapr.InvokeMethodAsync<ClassifiedLandingPageResponse>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"/api/{Vertical}/landing"
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
                    $"api/{Vertical}/category",
                    adCategory
                );
            }
            catch (Exception ex)
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
                    $"api/{Vertical}/categories",
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
                    $"api/{Vertical}/subcategory",
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
                    $"api/{Vertical}/subcategory",
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
                    $"api/{Vertical}subcategory/by-category/{categoryId}",
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
                    $"api/{Vertical}/brand",
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
                    $"api/{Vertical}/brand"
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
                    $"api/{Vertical}/brand/by-subcategory/{subCategoryId}"
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
                    $"api/{Vertical}/model",
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
                    $"api/{Vertical}/model",
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
                    $"api/{Vertical}/model/by-brand/{brandId}",
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
                    $"api/{Vertical}/condition",
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
                    $"api/{Vertical}/condition",
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
                    $"api/{Vertical}/color",
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
                    $"api/{Vertical}/color",
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
                    $"api/{Vertical}/capacity",
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
                    $"api/{Vertical}/capacity",
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
                    $"api/{Vertical}/processor",
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
                    $"api/{Vertical}/processor",
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
                    $"api/{Vertical}/processor/by-model/{modelId}",
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
                    $"api/{Vertical}/coverage",
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
                    $"api/{Vertical}/coverage",
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
                    $"api/{Vertical}/ram",
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
                    $"api/{Vertical}/ram",
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
                    $"api/{Vertical}/ram/by-model/{modelId}",
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
                    $"api/{Vertical}/resolution",
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
                    $"api/{Vertical}/resolution",
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
                    $"api/{Vertical}/resolution/by-model/{modelId}",
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
                    $"api/{Vertical}/size-type",
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
                    $"api/{Vertical}/size-type",
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
                    $"api/{Vertical}/gender",
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
                    $"api/{Vertical}/gender",
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
                    $"api/{Vertical}/zone",
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
                    $"api/{Vertical}/zone",
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

        public async Task<string> CreateAd(AdInformation ad, string userId, CancellationToken token = default)
        {
            try
            {
                var url = $"api/{Vertical}/ad";
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

        public async Task<List<AdResponse>> GetUserAds(string userId, bool? isPublished, CancellationToken token = default)
        {
            try
            {
                var url = $"api/{Vertical}/ad/user?isPublished={isPublished}";
                return await _dapr.InvokeMethodAsync<List<AdResponse>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    url,
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
