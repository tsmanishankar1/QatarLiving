using Dapr;
using Dapr.Client;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Spatial;
using QLN.Backend.API.Service.ProductService;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Classifieds;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using QLN.Common.Infrastructure.Utilities;
using System.Net;
using System.Text;
using System.Text.Json;
using static QLN.Common.DTO_s.ClassifiedsIndex;

namespace QLN.Backend.API.Service.ClassifiedService
{
    public class ExternalClassifiedService : IClassifiedService
    {
        private const string SERVICE_APP_ID = ConstantValues.ServiceAppIds.ClassifiedServiceApp;
        private const string Vertical = ConstantValues.ClassifiedsVertical;

        private readonly DaprClient _dapr;
        private readonly IEventlogger _log;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileStorageBlobService _fileStorageBlob;
        private readonly ISearchService _searchService;
        private readonly IV2SubscriptionService _subscriptionContext;


        public ExternalClassifiedService(DaprClient dapr, IEventlogger log, IHttpContextAccessor httpContextAccessor, IFileStorageBlobService fileStorageBlob, ISearchService searchService, IV2SubscriptionService subscriptionService)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _httpContextAccessor = httpContextAccessor;
            _fileStorageBlob = fileStorageBlob;
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _subscriptionContext = subscriptionService;
        }
        public async Task<bool> SaveSearch(SaveSearchRequestDto dto, string userId, CancellationToken cancellationToken = default)
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

        public async Task<AdCreatedResponseDto> CreateClassifiedItemsAd(Items dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (dto.Images == null || dto.Images.Count == 0)
                throw new ArgumentException("At least one ad image is required.");

            if (dto.SubVertical != SubVertical.Items)
                throw new InvalidOperationException("This endpoint only supports posting ads under the 'Items' subvertical.");
           

            try
            {
                _log.LogTrace($"Calling internal service with {dto.Images.Count} images");
                var requestUrl = $"/api/classifieds/items/post-by-id";
                var payload = JsonSerializer.Serialize(dto);
                var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, SERVICE_APP_ID, requestUrl);
                req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var res = await _dapr.InvokeMethodWithResponseAsync(req, cancellationToken);
                var body = await res.Content.ReadAsStringAsync(cancellationToken);
                if (!res.IsSuccessStatusCode)
                    throw new DaprServiceException((int)res.StatusCode, body);
                var createdDto = JsonSerializer.Deserialize<AdCreatedResponseDto>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _log.LogTrace($"Calling internal service with {dto} ");

                if (createdDto == null)
                    throw new InvalidOperationException("Failed to deserialize ad creation response.");

                return createdDto;              
            }
            catch(DaprServiceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdCreatedResponseDto> RefreshClassifiedItemsAd(
     SubVertical subVertical,
     long adId,
     string userId,
     Guid subscriptionId,
     CancellationToken cancellationToken)
        {
            if (adId <= 0)
            {
                throw new ArgumentException("AdId is required.");
            }

            HttpStatusCode? failedStatusCode = null;

            try
            {
                subscriptionId = Guid.Parse("5a024f96-7414-4473-80b8-f5d70297e262");

                if (subscriptionId != Guid.Empty)
                {
                    var canUse = await _subscriptionContext.ValidateSubscriptionUsageAsync(
                        subscriptionId,
                        "refresh",
                        1,
                        cancellationToken
                    );

                    if (!canUse)
                    {
                        _log.LogWarning(
                            "Subscription {SubscriptionId} has insufficient quota for refresh.",
                            GuidToLong(subscriptionId)
                        );
                        throw new InvalidOperationException("Insufficient subscription quota for refresh.");
                    }
                }

                var url = $"/api/classifieds/items/refreshed/{userId}/{adId}?subVertical={(int)subVertical}&subscriptionId={subscriptionId}";

                var serviceRequest = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, SERVICE_APP_ID, url);
                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;

                    if (!string.IsNullOrWhiteSpace(errorContent))
                    {
                        var trimmed = errorContent.TrimStart();
                        if (trimmed.StartsWith("{"))
                        {
                            try
                            {
                                var problem = JsonSerializer.Deserialize<ProblemDetails>(errorContent);
                                errorMessage = problem?.Detail ?? "Unknown validation error.";
                            }
                            catch (JsonException)
                            {
                                errorMessage = errorContent;
                            }
                        }
                        else
                        {
                            
                            errorMessage = errorContent;
                        }
                    }
                    else
                    {
                        errorMessage = "No error details returned from service.";
                    }

                    failedStatusCode = response.StatusCode;
                    throw new InvalidDataException(errorMessage);
                }

                
                if (subscriptionId != Guid.Empty)
                {
                    var success = await _subscriptionContext.RecordSubscriptionUsageAsync(
                        subscriptionId,
                        "refresh",
                        1,
                        cancellationToken
                    );

                    if (!success)
                    {
                        _log.LogWarning(
                            "Failed to record subscription usage for SubscriptionId {SubscriptionId}",
                            GuidToLong(subscriptionId)
                        );
                    }
                }

               
                return new AdCreatedResponseDto
                {
                    CreatedAt = DateTime.UtcNow,
                    AdId = adId,
                    Message = "Ad successfully refreshed."
                };
            }
            catch (ArgumentException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("AdId is required and must be valid.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
            catch (DaprException daprEx)
            {
                _log.LogException(daprEx);
                throw new InvalidOperationException("Failed to invoke internal service through Dapr.", daprEx);
            }
            catch (HttpRequestException httpEx)
            {
                _log.LogException(httpEx);
                throw new InvalidOperationException("Failed to communicate with the internal service.", httpEx);
            }
            catch (Exception ex)
            {
                if (failedStatusCode == HttpStatusCode.Conflict)
                {
                    throw new ConflictException(ex.Message);
                }
                else if (failedStatusCode == HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException(ex.Message);
                }
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to refresh the ad due to an unexpected error.", ex);
            }
        }



        public async Task<AdCreatedResponseDto> CreateClassifiedPrelovedAd(Preloveds dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (dto.Images == null || dto.Images.Count == 0)
                throw new ArgumentException("At least one ad image is required.");
            if (string.IsNullOrWhiteSpace(dto.AuthenticityCertificateUrl))
                throw new ArgumentException("Certificate image is required.");

            if(dto.SubVertical != SubVertical.Preloved)
                throw new InvalidOperationException("This endpoint only supports posting ads under the 'Preloved' subvertical.");

            try
            {
                _log.LogTrace($"Calling internal service with CertificateUrl: {dto.AuthenticityCertificateUrl} and {dto.Images.Count} images");
                var requestUrl = $"api/classifieds/preloved/post-by-id";
                var payload = JsonSerializer.Serialize(dto);
                var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, SERVICE_APP_ID, requestUrl);
                req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var res = await _dapr.InvokeMethodWithResponseAsync(req, cancellationToken);
                var body = await res.Content.ReadAsStringAsync(cancellationToken);
                if (!res.IsSuccessStatusCode)
                    throw new DaprServiceException((int)res.StatusCode, body);

                var createdDto = JsonSerializer.Deserialize<AdCreatedResponseDto>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (createdDto == null)
                    throw new InvalidOperationException("Failed to deserialize ad creation response.");

                return createdDto;
            }
            catch (DaprServiceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdCreatedResponseDto> CreateClassifiedCollectiblesAd(Collectibles dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (dto.Images == null || dto.Images.Count == 0)
                throw new ArgumentException("At least one ad image is required.");
            if (string.IsNullOrWhiteSpace(dto.AuthenticityCertificateUrl))
                throw new ArgumentException("Certificate image is required.");

            if (dto.SubVertical != SubVertical.Collectibles)
                throw new InvalidOperationException("This endpoint only supports posting ads under the 'Collectibles' subvertical.");

            try
            {             
                _log.LogTrace($"Calling internal collectibles service with {dto.Images.Count} images and cert: {dto.AuthenticityCertificateUrl}");
                var requestUrl = $"api/classifieds/collectibles/post-by-id";
                var payload = JsonSerializer.Serialize(dto);
                var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, SERVICE_APP_ID, requestUrl);
                req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var res = await _dapr.InvokeMethodWithResponseAsync(req, cancellationToken);
                var body = await res.Content.ReadAsStringAsync(cancellationToken);
                if (!res.IsSuccessStatusCode)
                    throw new DaprServiceException((int)res.StatusCode, body);

                var createdDto = JsonSerializer.Deserialize<AdCreatedResponseDto>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (createdDto == null)
                    throw new InvalidOperationException("Failed to deserialize ad creation response.");

                return createdDto;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdCreatedResponseDto> CreateClassifiedDealsAd(Deals dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Offertitle)) throw new ArgumentException("Title is required.");
            if (dto.Images == null || dto.Images.Count == 0)
                throw new ArgumentException("Ad image is required.");
            if (string.IsNullOrWhiteSpace(dto.FlyerFileUrl)) throw new ArgumentException("FlyerFile image is required.");
            

            try
            {               
                var requestUrl = $"api/classifieds/deals/post-by-id";
                var payload = JsonSerializer.Serialize(dto);
                var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, SERVICE_APP_ID, requestUrl);
                req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var res = await _dapr.InvokeMethodWithResponseAsync(req, cancellationToken);
                var body = await res.Content.ReadAsStringAsync(cancellationToken);
                if (!res.IsSuccessStatusCode)
                    throw new DaprServiceException((int)res.StatusCode, body);

                var createdResponse = JsonSerializer.Deserialize<AdCreatedResponseDto>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return createdResponse ?? throw new InvalidOperationException("Invalid response from internal service.");

            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }
          
        public async Task<DeleteAdResponseDto> DeleteClassifiedAd(SubVertical subVertical, long adId, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (adId <= 0)
                    throw new ArgumentException("Ad ID must be a valid positive number.", nameof(adId));

                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("UserId must not be empty.", nameof(userId));

                var response = await _dapr.InvokeMethodAsync<DeleteAdResponseDto>(
                    HttpMethod.Delete,
                    SERVICE_APP_ID,
                    $"api/classifieds/{subVertical}/delete-by-id/{adId}/{userId}",
                    cancellationToken
                );
                
                string indexName = subVertical switch
                {
                    SubVertical.Items => ConstantValues.IndexNames.ClassifiedsItemsIndex,
                    SubVertical.Preloved => ConstantValues.IndexNames.ClassifiedsPrelovedIndex,
                    SubVertical.Collectibles => ConstantValues.IndexNames.ClassifiedsCollectiblesIndex,
                    SubVertical.Deals => ConstantValues.IndexNames.ClassifiedsDealsIndex,
                    _ => throw new InvalidOperationException($"Unsupported subVertical: {subVertical}")
                };

                await _searchService.DeleteAsync(indexName, adId.ToString());

                return response;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException($"Failed to delete classified ad from subvertical {subVertical}.", ex);
            }
        }

        public async Task<Items> GetItemAdById(long adId, CancellationToken cancellationToken = default)
        {
            if (adId <= 0)
                throw new ArgumentException("Ad ID must not be empty.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<Items>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/items/{adId}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException($"Failed to retrieve ad details for Ad ID: {adId} from classified microservice.", ex);
            }
            catch(KeyNotFoundException ex)
            {
                _log.LogException(ex);
                throw new KeyNotFoundException($"Ad with key {adId} does not exist.");
            }
        }

        public async Task<List<Items>> GetAllItemsAdByUser(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserID must not be empty.");
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<Items>>(
                 HttpMethod.Get,
                 SERVICE_APP_ID,
                 $"api/classifieds/items/by-user/{userId}",
                 cancellationToken);

                return result;
            }
            catch(Exception ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException($"Failed to retrieve ad details for User from classified microservice.", ex);
            }
        }

        public async Task<List<Preloveds>> GetAllPrelovedAdByUser(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserID must not be empty.");
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<Preloveds>>(
                 HttpMethod.Get,
                 SERVICE_APP_ID,
                 $"api/classifieds/preloved/by-user/{userId}",
                 cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException($"Failed to retrieve ad details for User from classified microservice.", ex);
            }
        }

        public async Task<List<Collectibles>> GetAllCollectiblesAdByUser(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserID must not be empty.");
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<Collectibles>>(
                 HttpMethod.Get,
                 SERVICE_APP_ID,
                 $"api/classifieds/collectibles/by-user/{userId}",
                 cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException($"Failed to retrieve ad details for User from classified microservice.", ex);
            }
        }

        public async Task<List<Deals>> GetAllDealsAdByUser(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserID must not be empty.");
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<Deals>>(
                 HttpMethod.Get,
                 SERVICE_APP_ID,
                 $"api/classifieds/deals/by-user/{userId}",
                 cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException($"Failed to retrieve ad details for User from classified microservice.", ex);
            }
        }

        public async Task<Preloveds> GetPrelovedAdById(long adId, CancellationToken cancellationToken = default)
        {
            if (adId <= 0)
                throw new ArgumentException("Ad ID must not be empty.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<Preloveds>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/preloved/{adId}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve Preloved ad from classified microservice.", ex);
            }
            catch (KeyNotFoundException ex)
            {
                _log.LogException(ex);
                throw new KeyNotFoundException($"Ad with key {adId} does not exist.");
            }
        }

        public async Task<Deals> GetDealsAdById(long adId, CancellationToken cancellationToken = default)
        {
            if (adId <= 0)
                throw new ArgumentException("Ad ID must not be empty.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<Deals>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/deals/{adId}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve Deals ad from classified microservice.", ex);
            }
            catch (KeyNotFoundException ex)
            {
                _log.LogException(ex);
                throw new KeyNotFoundException($"Ad with key {adId} does not exist.");
            }
        }

        public async Task<Collectibles> GetCollectiblesAdById(long adId, CancellationToken cancellationToken = default)
        {
            if (adId <= 0)
                throw new ArgumentException("Ad ID must not be empty.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<Collectibles>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/collectibles/{adId}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve Collectibles ad from classified microservice.", ex);
            }
            catch (KeyNotFoundException ex)
            {
                _log.LogException(ex);
                throw new KeyNotFoundException($"Ad with key {adId} does not exist.");
            }
        }

        public async Task<Guid> CreateCategory(CategoryDtos dto, CancellationToken cancellationToken)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Category data must not be null.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Category name must not be empty.");

            try
            {
                var response = await _dapr.InvokeMethodAsync<CategoryDtos, Guid>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/classifieds/category",
                    dto,
                    cancellationToken);

                return response;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to create category in classified microservice.", ex);
            }
        }

        public async Task<List<Categories>> GetChildCategories(string vertical, Guid parentId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical must be specified.");

            if (parentId == Guid.Empty)
                throw new ArgumentException("Parent category ID must not be empty.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<List<Categories>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/category/{vertical}/{parentId}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve child categories from classified microservice.", ex);
            }
        }

        public async Task<CategoryTreeDto?> GetCategoryTree(string vertical, Guid categoryId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical must be specified.");

            if (categoryId == Guid.Empty)
                throw new ArgumentException("Category ID must not be empty.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<CategoryTreeDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/category/tree/{vertical}/{categoryId}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve category hierarchy tree from classified microservice.", ex);
            }
        }

        public async Task DeleteCategoryTree(string vertical, Guid categoryId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical must be specified.");

            if (categoryId == Guid.Empty)
                throw new ArgumentException("Category ID must not be empty.");

            try
            {
                await _dapr.InvokeMethodAsync(
                    HttpMethod.Delete,
                    SERVICE_APP_ID,
                    $"api/classifieds/category/{vertical}/{categoryId}/tree",
                    cancellationToken);
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to delete category tree from classified microservice.", ex);
            }
        }

        public async Task<List<CategoryTreeDto>> GetAllCategoryTrees(string vertical, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical must be specified.");
            try
            {
                var result = await _dapr.InvokeMethodAsync<List<CategoryTreeDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/category/{vertical}/all-trees",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve category tree list from classified microservice.", ex);
            }
        }

        public async Task<List<CategoryField>> GetFiltersByMainCategoryAsync(string vertical, Guid mainCategoryId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical must be specified.");

            if (mainCategoryId == Guid.Empty)
                throw new ArgumentException("Main category ID must not be empty.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<List<CategoryField>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/category/{vertical}/{mainCategoryId}/filters",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve category filters from classified microservice.", ex);
            }
        }

        public async Task<AdUpdatedResponseDto> UpdateClassifiedItemsAd(Items dto, CancellationToken cancellationToken = default)
        {
            if (dto.Id == null)
                throw new ArgumentException("Id must be specified.", nameof(dto.Id));

            try
            {

                var response = await _dapr.InvokeMethodAsync<Items, AdUpdatedResponseDto>(
                    HttpMethod.Put,
                    SERVICE_APP_ID,
                    "api/classifieds/items/update-by-id",
                    dto,
                    cancellationToken);
                return response;
            }
            catch (InvocationException ex)
            {
                _log.LogError(ex, "Dapr invocation failed for Ad ID {AdId}", dto.Id);
                throw new InvalidOperationException("Error while invoking classifieds microservice.", ex);
            }
            catch (HttpRequestException ex)
            {
                _log.LogError(ex, "HTTP request failed while updating Ad ID {AdId}", dto.Id);
                throw new InvalidOperationException("HTTP connection to classifieds microservice failed.", ex);
            }
            catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                _log.LogWarning("Operation cancelled for Ad ID: {AdId}", dto.Id);
                throw new OperationCanceledException("The request was cancelled.", ex, cancellationToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected exception while updating Ad ID {AdId}", dto.Id);
                throw;
            }
        }

        public async Task<AdUpdatedResponseDto> UpdateClassifiedPrelovedAd(Preloveds dto, CancellationToken cancellationToken = default)
        {
            if (dto.Id == null)
            {
                throw new ArgumentException("Id must be specified.");
            }
            try
            {
                var response = await _dapr.InvokeMethodAsync<Preloveds, AdUpdatedResponseDto>(
                HttpMethod.Put,
                SERVICE_APP_ID,
                $"api/classifieds/preloved/update-by-id",
                dto,
                cancellationToken);
                

                return response;
            }
            catch (InvocationException ex)
            {
                _log.LogError(ex, "Dapr invocation failed for Ad ID {AdId}", dto.Id);
                throw new InvalidOperationException("Error while invoking classifieds microservice.", ex);
            }
            catch (HttpRequestException ex)
            {
                _log.LogError(ex, "HTTP request failed while updating Ad ID {AdId}", dto.Id);
                throw new InvalidOperationException("HTTP connection to classifieds microservice failed.", ex);
            }
            catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                _log.LogWarning("Operation cancelled for Ad ID: {AdId}", dto.Id);
                throw new OperationCanceledException("The request was cancelled.", ex, cancellationToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected exception while updating Ad ID {AdId}", dto.Id);
                throw;
            }
        }

        public async Task<AdUpdatedResponseDto> UpdateClassifiedCollectiblesAd(Collectibles dto, CancellationToken cancellationToken = default)
        {
            if (dto.Id == null)
            {
                throw new ArgumentException("Id must be specified.");
            }
            try
            {
                var response = await _dapr.InvokeMethodAsync<Collectibles, AdUpdatedResponseDto>(
                    HttpMethod.Put,
                    SERVICE_APP_ID,
                    $"api/classifieds/collectibles/update-by-id",
                    dto,
                    cancellationToken);
                
                return response;
            }
            catch (InvocationException ex)
            {
                _log.LogError(ex, "Dapr invocation failed for Ad ID {AdId}", dto.Id);
                throw new InvalidOperationException("Error while invoking classifieds microservice.", ex);
            }
            catch (HttpRequestException ex)
            {
                _log.LogError(ex, "HTTP request failed while updating Ad ID {AdId}", dto.Id);
                throw new InvalidOperationException("HTTP connection to classifieds microservice failed.", ex);
            }
            catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                _log.LogWarning("Operation cancelled for Ad ID: {AdId}", dto.Id);
                throw new OperationCanceledException("The request was cancelled.", ex, cancellationToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected exception while updating Ad ID {AdId}", dto.Id);
                throw;
            }
        }

        public async Task<AdUpdatedResponseDto> UpdateClassifiedDealsAd(Deals dto, CancellationToken cancellationToken = default)
        {
            if (dto.Id <= 0)
            {
                throw new ArgumentException("Id must be specified.");
            }

            try
            {
                var response = await _dapr.InvokeMethodAsync<Deals, AdUpdatedResponseDto>(
                    HttpMethod.Put,
                    SERVICE_APP_ID,
                    $"api/classifieds/deals/update-by-id",
                    dto,
                    cancellationToken);

                return response;
            }
            catch (InvocationException ex)
            {
                _log.LogError(ex, "Dapr invocation failed for Ad ID {AdId}", dto.Id);
                throw new InvalidOperationException("Error while invoking classifieds microservice.", ex);
            }
            catch (HttpRequestException ex)
            {
                _log.LogError(ex, "HTTP request failed while updating Ad ID {AdId}", dto.Id);
                throw new InvalidOperationException("HTTP connection to classifieds microservice failed.", ex);
            }
            catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                _log.LogWarning("Operation cancelled for Ad ID: {AdId}", dto.Id);
                throw new OperationCanceledException("The request was cancelled.", ex, cancellationToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected exception while updating Ad ID {AdId}", dto.Id);
                throw;
            }
        }

        public async Task<string> PromoteClassifiedAd(ClassifiedsPromoteDto dto, string userId, Guid subscriptionid, CancellationToken cancellationToken = default)
        {
            if (dto.AdId <= 0)
            {
                throw new ArgumentException("AdId is required.");
            }

            HttpStatusCode? failedStatusCode = null;

            try
            {
                subscriptionid = Guid.Parse("5a024f96-7414-4473-80b8-f5d70297e262");


                if (subscriptionid != Guid.Empty)
                {
                    var canUse = await _subscriptionContext.ValidateSubscriptionUsageAsync(
                        subscriptionid,
                        "Promote",
                        1,
                        cancellationToken
                    );

                    if (!canUse)
                    {
                        _log.LogWarning(
                            "Subscription {SubscriptionId} has insufficient quota for promotion.", GuidToLong(subscriptionid)
                        );
                        throw new InvalidOperationException("Insufficient subscription quota for promotion.");
                    }
                }


                var subVerticalStr = ((int)dto.SubVertical).ToString();
                var url = $"/api/classifieds/promoted/{userId}/{dto.AdId}?subVertical={subVerticalStr}&subscriptionId={subscriptionid}";
                var serviceRequest = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, SERVICE_APP_ID, url);
                serviceRequest.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;

                    if (!string.IsNullOrWhiteSpace(errorJson))
                    {
                        try
                        {
                            var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                            errorMessage = problem?.Detail ?? "Unknown validation error.";
                        }
                        catch (JsonException)
                        {
                            errorMessage = errorJson;
                        }
                    }
                    else
                    {
                        errorMessage = "No error details returned from service.";
                    }

                    failedStatusCode = response.StatusCode;
                    throw new InvalidDataException(errorMessage);
                }


                if (subscriptionid != Guid.Empty)
                {
                    var success = await _subscriptionContext.RecordSubscriptionUsageAsync(
                        subscriptionid,
                        "Promote",
                        1,
                        cancellationToken
                    );

                    if (!success)
                    {
                        _log.LogWarning(
                            "Failed to record subscription usage for SubscriptionId {SubscriptionId}",
                           GuidToLong(subscriptionid)
                        );
                    }
                }

                return "The ad has been successfully marked as promoted.";
            }
            catch (ArgumentException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("AdId is required and must be valid.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
            catch (DaprException daprEx)
            {
                _log.LogException(daprEx);
                throw new InvalidOperationException("Failed to invoke internal service through Dapr.", daprEx);
            }
            catch (HttpRequestException httpEx)
            {
                _log.LogException(httpEx);
                throw new InvalidOperationException("Failed to communicate with the internal service.", httpEx);
            }
            catch (Exception ex)
            {
                if (failedStatusCode == HttpStatusCode.Conflict)
                {
                    throw new ConflictException(ex.Message);
                }
                else if (failedStatusCode == HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException(ex.Message);
                }
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to promote the ad due to an unexpected error.", ex);
            }
        }

        public async Task<string> FeatureClassifiedAd(ClassifiedsPromoteDto dto, string userId, Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            if (dto.AdId <= 0)
            {
                throw new ArgumentException("AdId is required.");
            }

            HttpStatusCode? failedStatusCode = null;

            try
            {
               var subscriptionid = Guid.Parse("5a024f96-7414-4473-80b8-f5d70297e262");

                if (subscriptionid != Guid.Empty)
                {
                    var canUse = await _subscriptionContext.ValidateSubscriptionUsageAsync(
                        subscriptionid,
                        "feature",
                        1,
                        cancellationToken
                    );

                    if (!canUse)
                    {
                        _log.LogWarning(
                            "Subscription {SubscriptionId} has insufficient quota for feature.",
                            GuidToLong(subscriptionid)
                        );
                        throw new InvalidOperationException("Insufficient subscription quota for feature.");
                    }
                }
                var queryParams = new Dictionary<string, string>
{
    { "adId", dto.AdId.ToString() },
    { "subVertical", ((int)dto.SubVertical).ToString() },
    { "userId", userId }
};

                var queryString = "?" + string.Join("&", queryParams.Select(kv =>
                    $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

                var url = $"/api/classifieds/featured/{queryString}&subscriptionId={subscriptionid}";



                //var subVerticalStr = ((int)dto.SubVertical).ToString();
                //var url = $"/api/classifieds/items/featured/{userId}/{dto.AdId}?subVertical={subVerticalStr}";

                var serviceRequest = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, SERVICE_APP_ID, url);
                serviceRequest.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;

                    if (!string.IsNullOrWhiteSpace(errorJson))
                    {
                        try
                        {
                            var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                            errorMessage = problem?.Detail ?? "Unknown validation error.";
                        }
                        catch (JsonException)
                        {
                            errorMessage = errorJson;
                        }
                    }
                    else
                    {
                        errorMessage = "No error details returned from service.";
                    }

                    failedStatusCode = response.StatusCode;
                    throw new InvalidDataException(errorMessage);
                }

                
                if (subscriptionid != Guid.Empty)
                {
                    var success = await _subscriptionContext.RecordSubscriptionUsageAsync(
                        subscriptionid,
                        "feature",
                        1,
                        cancellationToken
                    );

                    if (!success)
                    {
                        _log.LogWarning(
                            "Failed to record subscription usage for SubscriptionId {SubscriptionId}",
                            GuidToLong(subscriptionid)
                        );
                    }
                }

                return "The ad has been successfully marked as featured.";
            }
            catch (ArgumentException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("AdId is required and must be valid.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
            catch (DaprException daprEx)
            {
                _log.LogException(daprEx);
                throw new InvalidOperationException("Failed to invoke internal service through Dapr.", daprEx);
            }
            catch (HttpRequestException httpEx)
            {
                _log.LogException(httpEx);
                throw new InvalidOperationException("Failed to communicate with the internal service.", httpEx);
            }
            catch (Exception ex)
            {
                if (failedStatusCode == HttpStatusCode.Conflict)
                {
                    throw new ConflictException(ex.Message);
                }
                else if (failedStatusCode == HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException(ex.Message);
                }
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to feature the ad due to an unexpected error.", ex);
            }
        }

        public async Task<string> UnFeatureClassifiedAd(ClassifiedsPromoteDto dto, string userId, Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            if (dto.AdId <= 0)
            {
                throw new ArgumentException("AdId is required.");
            }

            HttpStatusCode? failedStatusCode = null;

            try
            {
                var subscriptionid = Guid.Parse("5a024f96-7414-4473-80b8-f5d70297e262");

                if (subscriptionid != Guid.Empty)
                {
                    var canUse = await _subscriptionContext.ValidateSubscriptionUsageAsync(
                        subscriptionid,
                        "unfeature",
                        1,
                        cancellationToken
                    );

                    if (!canUse)
                    {
                        _log.LogWarning(
                            "Subscription {SubscriptionId} has insufficient quota for feature.",
                            GuidToLong(subscriptionid)
                        );
                        throw new InvalidOperationException("Insufficient subscription quota for feature.");
                    }
                }
                var queryParams = new Dictionary<string, string>
{
    { "adId", dto.AdId.ToString() },
    { "subVertical", ((int)dto.SubVertical).ToString() },
    { "userId", userId }
};

                var queryString = "?" + string.Join("&", queryParams.Select(kv =>
                    $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

                var url = $"/api/classifieds/unfeatured/{queryString}&subscriptionId={subscriptionid}";



                //var subVerticalStr = ((int)dto.SubVertical).ToString();
                //var url = $"/api/classifieds/items/featured/{userId}/{dto.AdId}?subVertical={subVerticalStr}";

                var serviceRequest = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, SERVICE_APP_ID, url);
                serviceRequest.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;

                    if (!string.IsNullOrWhiteSpace(errorJson))
                    {
                        try
                        {
                            var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                            errorMessage = problem?.Detail ?? "Unknown validation error.";
                        }
                        catch (JsonException)
                        {
                            errorMessage = errorJson;
                        }
                    }
                    else
                    {
                        errorMessage = "No error details returned from service.";
                    }

                    failedStatusCode = response.StatusCode;
                    throw new InvalidDataException(errorMessage);
                }


                if (subscriptionid != Guid.Empty)
                {
                    var success = await _subscriptionContext.RecordSubscriptionUsageAsync(
                        subscriptionid,
                        "unfeature",
                        1,
                        cancellationToken
                    );

                    if (!success)
                    {
                        _log.LogWarning(
                            "Failed to record subscription usage for SubscriptionId {SubscriptionId}",
                            GuidToLong(subscriptionid)
                        );
                    }
                }

                return "The ad has been successfully marked as featured.";
            }
            catch (ArgumentException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("AdId is required and must be valid.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
            catch (DaprException daprEx)
            {
                _log.LogException(daprEx);
                throw new InvalidOperationException("Failed to invoke internal service through Dapr.", daprEx);
            }
            catch (HttpRequestException httpEx)
            {
                _log.LogException(httpEx);
                throw new InvalidOperationException("Failed to communicate with the internal service.", httpEx);
            }
            catch (Exception ex)
            {
                if (failedStatusCode == HttpStatusCode.Conflict)
                {
                    throw new ConflictException(ex.Message);
                }
                else if (failedStatusCode == HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException(ex.Message);
                }
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to feature the ad due to an unexpected error.", ex);
            }
        }
        public async Task<PaginatedAdResponseDto> GetFilteredAds(SubVertical subVertical, bool? isPublished,int page,int pageSize,string? search,string userId,CancellationToken cancellationToken = default)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(userId))
                        throw new ArgumentException("User ID must not be empty.", nameof(userId));

                    var queryParams = new Dictionary<string, string>
                    {
                        { "subVertical", ((int)subVertical).ToString()  },
                        { "page", page.ToString() },
                        { "pageSize", pageSize.ToString() },
                        { "userId", userId } 
                    };

                    if (isPublished.HasValue)
                        queryParams["isPublished"] = isPublished.Value.ToString();

                    if (!string.IsNullOrWhiteSpace(search))
                        queryParams["search"] = search;

                    var queryString = "?" + string.Join("&", queryParams.Select(kv =>
                        $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

                    var result = await _dapr.InvokeMethodAsync<PaginatedAdResponseDto>(
                        HttpMethod.Get,
                        SERVICE_APP_ID,
                        $"api/classifieds/user-dashborad/{userId}/{queryString}",
                        cancellationToken);

                    return result;
                }
                catch (InvocationException ex)
                {
                    throw;
                }
            }
        public async Task<BulkAdActionResponse> BulkUpdateAdPublishStatusAsync(
     int subVertical,
     string userId,
     List<long> adIds,
     bool isPublished,
     CancellationToken cancellationToken = default)
        {
            if (subVertical <= 0 || string.IsNullOrWhiteSpace(userId) || adIds == null || adIds.Count == 0)
            {
                throw new ArgumentException("Invalid bulk publish/unpublish request.");
            }

            try
            {
                // Convert int to string when building route
                var subVerticalStr = subVertical.ToString();

                var route =
                    $"api/classifieds/user-dashboard/bulk-action-by-id" +
                    $"?subVertical={Uri.EscapeDataString(subVerticalStr)}" +
                    $"&isPublished={isPublished.ToString().ToLowerInvariant()}" +
                    $"&userId={Uri.EscapeDataString(userId)}";

                var result = await _dapr.InvokeMethodAsync<List<long>, BulkAdActionResponse>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    route,
                    adIds,
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException(
                    "Failed to update ad publish status from classified microservice.",
                    ex
                );
            }
        }
        private static long GuidToLong(Guid guid)
        {
            var bytes = guid.ToByteArray();
            return BitConverter.ToInt64(bytes, 0); // uses first 8 bytes
        }

        public Task<string> MigrateClassifiedItemsAd(Items dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
          
        public Task<string> MigrateClassifiedCollectiblesAd(Collectibles dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<string> Favourite(WishlistCreateDto dto, string userId, CancellationToken cancellationToken = default)
        {
            if (dto.AdId <= 0)
            {
                throw new ArgumentException("AdId is required.");
            }

            HttpStatusCode? failedStatusCode = null;

            try
            {
                var queryParams = new Dictionary<string, string>
                {
                    { "adId", dto.AdId.ToString() },
                    { "vertical", ((int)dto.Vertical).ToString() },
                    { "userId", userId }
                };

                var queryString = "?" + string.Join("&", queryParams.Select(kv =>
                    $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

                var url = $"/api/classifieds/wishlist/favourite-by-id{queryString}";

                var serviceRequest = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, SERVICE_APP_ID, url);
                serviceRequest.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;

                    if (!string.IsNullOrWhiteSpace(errorJson))
                    {
                        try
                        {
                            var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                            errorMessage = problem?.Detail ?? "Unknown validation error.";
                        }
                        catch (JsonException)
                        {
                            errorMessage = errorJson;
                        }
                    }
                    else
                    {
                        errorMessage = "No error details returned from service.";
                    }

                    failedStatusCode = response.StatusCode;
                    throw new InvalidDataException(errorMessage);
                }

                var successMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                return string.IsNullOrWhiteSpace(successMessage)
                    ? "Added to favourites successfully."
                    : successMessage;
            }
            catch (ArgumentException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("AdId is required and must be valid.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
            catch (DaprException daprEx)
            {
                _log.LogException(daprEx);
                throw new InvalidOperationException("Failed to invoke internal service through Dapr.", daprEx);
            }
            catch (HttpRequestException httpEx)
            {
                _log.LogException(httpEx);
                throw new InvalidOperationException("Failed to communicate with the internal service.", httpEx);
            }
            catch (Exception ex)
            {
                if (failedStatusCode == HttpStatusCode.Conflict)
                {
                    throw new ConflictException(ex.Message);
                }
                else if (failedStatusCode == HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException(ex.Message);
                }
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to add to favourites due to an unexpected error.", ex);
            }
        }

        public async Task<List<Wishlist>> GetAllByUserFavouriteList(string userId, Vertical vertical, SubVertical subVertical, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID must not be empty.", nameof(userId));

                //var queryParams = new Dictionary<string, string>
                //{
                //    { "vertical", ((int)vertical).ToString() },
                //    { "subVertical", ((int)subVertical).ToString() },
                //    { "userId", userId }
                //};

                var queryString = $"?userId={Uri.EscapeDataString(userId)}" +
                          $"&vertical={(int)vertical}" +
                          $"&subVertical={(int)subVertical}";

                var result = await _dapr.InvokeMethodAsync<List<Wishlist>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID, 
                    $"api/classifieds/wishlist/list-by-id{queryString}",
                    cancellationToken);

                return result ?? new List<Wishlist>(); 
            }
            catch (InvocationException ex)
            {
                throw new InvalidOperationException("Failed to retrieve wishlist from external service.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while retrieving wishlist.", ex);
            }
        }

        public async Task<string> UnFavourite(string userId, Vertical vertical, SubVertical subVertical, long adId, CancellationToken cancellationToken = default)
        {
            if (adId <= 0)
            {
                throw new ArgumentException("AdId is required and must be greater than zero.");
            }

            HttpStatusCode? failedStatusCode = null;

            try
            {
                var queryParams = new Dictionary<string, string>
                {
                    { "userId", userId },
                    { "vertical", ((int)vertical).ToString() },
                    { "subVertical", ((int)subVertical).ToString() },
                    { "adId", adId.ToString() }
                };

                var queryString = "?" + string.Join("&", queryParams.Select(kv =>
                    $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
                
                var url = $"/api/classifieds/wishlist/unfavourite-by-id{queryString}";

                var serviceRequest = _dapr.CreateInvokeMethodRequest(HttpMethod.Delete, SERVICE_APP_ID, url);

                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;

                    if (!string.IsNullOrWhiteSpace(errorJson))
                    {
                        try
                        {
                            var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                            errorMessage = problem?.Detail ?? "Unknown validation error.";
                        }
                        catch (JsonException)
                        {
                            errorMessage = errorJson;
                        }
                    }
                    else
                    {
                        errorMessage = "No error details returned from service.";
                    }

                    failedStatusCode = response.StatusCode;
                    throw new InvalidDataException(errorMessage);
                }

                var successMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                return string.IsNullOrWhiteSpace(successMessage)
                    ? "Wishlist item removed successfully."
                    : successMessage;
            }
            catch (ArgumentException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("AdId is required and must be valid.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
            catch (DaprException daprEx)
            {
                _log.LogException(daprEx);
                throw new InvalidOperationException("Failed to invoke internal service through Dapr.", daprEx);
            }
            catch (HttpRequestException httpEx)
            {
                _log.LogException(httpEx);
                throw new InvalidOperationException("Failed to communicate with the internal service.", httpEx);
            }
            catch (Exception ex)
            {
                if (failedStatusCode == HttpStatusCode.Conflict)
                {
                    throw new ConflictException(ex.Message);
                }
                else if (failedStatusCode == HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException(ex.Message);
                }
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to remove from favourites due to an unexpected error.", ex);
            }
        }

    }
}

