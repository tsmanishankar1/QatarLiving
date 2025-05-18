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

        public async Task<Adcateg> AddCategory(string categoryName, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<string, Adcateg>(
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

        public async Task<List<Adcateg>> GetAllCategories(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<Adcateg>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/categories",
                    cancellationToken
                    );

                return result ?? new List<Adcateg>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdSubCategory> AddSubCategory(string name, Guid categoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new
                {
                    Name = name,
                    CategoryId = categoryId
                };

                return await _dapr.InvokeMethodAsync<object, AdSubCategory>(
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

        public async Task<List<AdSubCategory>> GetSubCategoriesByCategoryId(Guid categoryId)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<AdSubCategory>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/subcategory/by-category/{categoryId}"                    
                );

                return result ?? new List<AdSubCategory>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdBrand> AddBrand(string name, Guid subCategoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new
                {
                    Name = name,
                    SubCategoryId = subCategoryId
                };

                return await _dapr.InvokeMethodAsync<object, AdBrand>(
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

        public async Task<List<AdBrand>> GetAllBrands(CancellationToken cancellationToken = default)
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

        public async Task<List<AdBrand>> GetBrandsBySubCategoryId(Guid subCategoryId, CancellationToken cancellationToken = default)
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

        public async Task<AdModel> AddModel(string name, Guid brandId, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name, BrandId = brandId };

                return await _dapr.InvokeMethodAsync<object, AdModel>(
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

        public async Task<AdCondition> AddCondition(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name };

                return await _dapr.InvokeMethodAsync<object, AdCondition>(
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

        public async Task<AdColor> AddColor(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name };

                return await _dapr.InvokeMethodAsync<object, AdColor>(
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

        public async Task<AdCapacity> AddCapacity(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name };

                return await _dapr.InvokeMethodAsync<object, AdCapacity>(
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

        public async Task<AdProcessor> AddProcessor(string name, Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name, ModelId = modelId };

                return await _dapr.InvokeMethodAsync<object, AdProcessor>(
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

        public async Task<AdCoverage> AddCoverage(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name };

                return await _dapr.InvokeMethodAsync<object, AdCoverage>(
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
        public async Task<AdRam> AddRam(string name, Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name, ModelId = modelId };

                return await _dapr.InvokeMethodAsync<object, AdRam>(
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

        public async Task<AdResolution> AddResolution(string name, Guid modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name, ModelId = modelId };

                return await _dapr.InvokeMethodAsync<object, AdResolution>(
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

        public async Task<AdSizeType> AddSizeType(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name };

                return await _dapr.InvokeMethodAsync<object, AdSizeType>(
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

        public async Task<AdGender> AddGender(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name };

                return await _dapr.InvokeMethodAsync<object, AdGender>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/gender",
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

        public async Task<AdZone> AddZone(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { Name = name };

                return await _dapr.InvokeMethodAsync<object, AdZone>(
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

        //public async Task<string> CreateAd(AdInformation ad, string userId, CancellationToken token = default)
        //{
        //    try
        //    {
        //        var url = $"api/{Vertical}/ad";
        //        return await _dapr.InvokeMethodAsync<AdInformation, string>(
        //            HttpMethod.Post,
        //            SERVICE_APP_ID,
        //            url,
        //            ad,
        //            cancellationToken: token);
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.LogException(ex);
        //        throw;
        //    }
        //}

        //public async Task<List<AdResponse>> GetUserAds(string userId, bool? isPublished, CancellationToken token = default)
        //{
        //    try
        //    {
        //        var url = $"api/{Vertical}/ad/user?isPublished={isPublished}";
        //        return await _dapr.InvokeMethodAsync<List<AdResponse>>(
        //            HttpMethod.Get,
        //            SERVICE_APP_ID,
        //            url,
        //            cancellationToken: token);
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.LogException(ex);
        //        throw;
        //    }

        }
}
