using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Spatial;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Classifieds;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.Model;
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

        public ExternalClassifiedService(DaprClient dapr, IEventlogger log, IHttpContextAccessor httpContextAccessor, IFileStorageBlobService fileStorageBlob, ISearchService searchService)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _httpContextAccessor = httpContextAccessor;
            _fileStorageBlob = fileStorageBlob;
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
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

        public async Task<AdCreatedResponseDto> CreateClassifiedItemsAd(ClassifiedsItems dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (dto.Images == null || dto.Images.Count == 0)
                throw new ArgumentException("At least one ad image is required.");

            if (!string.Equals(dto.SubVertical, "Items", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This endpoint only supports posting ads under the 'Items' subvertical.");

            try
            {
                dto.Id = Guid.NewGuid();
                _log.LogTrace($"Calling internal service with {dto.Images.Count} images");
                var requestUrl = $"/api/classifieds/items/post-by-id";
                var payload = JsonSerializer.Serialize(dto);
                var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, SERVICE_APP_ID, requestUrl);
                req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var res = await _dapr.InvokeMethodWithResponseAsync(req, cancellationToken);
                var body = await res.Content.ReadAsStringAsync(cancellationToken);
                if (!res.IsSuccessStatusCode)
                    throw new DaprServiceException((int)res.StatusCode, body);
                return new AdCreatedResponseDto
                {
                    AdId = dto.Id,
                    Title = dto.Title,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Items Ad created successfully"
                };
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

        public async Task<AdCreatedResponseDto> RefreshClassifiedItemsAd(SubVertical subVertical, Guid adId, CancellationToken cancellationToken = default)
        {
            if (adId == Guid.Empty)
            {
                throw new ArgumentException("AdId is required.");
            }

            try
            {
                await _dapr.InvokeMethodAsync(
                   HttpMethod.Post,
                   SERVICE_APP_ID,
                   $"/api/classifieds/items/refresh/{adId}?subVertical={subVertical}",
                   cancellationToken
               );
                var item = await _searchService.GetByIdAsync<ClassifiedsIndex>(ConstantValues.Verticals.Classifieds, adId.ToString());
                if (item == null)
                {
                    throw new InvalidOperationException($"Ad with ID {adId} not found.");
                }

                item.IsRefreshed = true;
                item.CreatedDate = DateTime.UtcNow;
                item.RefreshExpiryDate = DateTime.UtcNow.AddHours(72);
                return new AdCreatedResponseDto
                {
                    AdId = adId,
                    Message = "Ad successfully refreshed."
                };
            }
            catch (ArgumentException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("AdId is required and must be valid.", ex);
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
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to refresh the ad due to an unexpected error.", ex);
            }
        }

        public async Task<AdCreatedResponseDto> CreateClassifiedPrelovedAd(ClassifiedsPreloved dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (dto.Images == null || dto.Images.Count == 0)
                throw new ArgumentException("At least one ad image is required.");
            if (string.IsNullOrWhiteSpace(dto.AuthenticityCertificateUrl))
                throw new ArgumentException("Certificate image is required.");

            if (!string.Equals(dto.SubVertical, "Preloved", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This endpoint only supports posting ads under the 'Preloved' subvertical.");

            try
            {
                dto.Id = Guid.NewGuid();
                _log.LogTrace($"Calling internal service with CertificateUrl: {dto.AuthenticityCertificateUrl} and {dto.Images.Count} images");
                var requestUrl = $"api/classifieds/preloved/post-by-id";
                var payload = JsonSerializer.Serialize(dto);
                var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, SERVICE_APP_ID, requestUrl);
                req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var res = await _dapr.InvokeMethodWithResponseAsync(req, cancellationToken);
                var body = await res.Content.ReadAsStringAsync(cancellationToken);
                if (!res.IsSuccessStatusCode)
                    throw new DaprServiceException((int)res.StatusCode, body);

              
                
                return new AdCreatedResponseDto
                {
                    AdId = dto.Id,
                    Title = dto.Title,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Preloved Ad created successfully"
                };
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

        public async Task<AdCreatedResponseDto> CreateClassifiedCollectiblesAd(ClassifiedsCollectibles dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (dto.Images == null || dto.Images.Count == 0)
                throw new ArgumentException("At least one ad image is required.");
            if (dto.HasAuthenticityCertificate == true && string.IsNullOrWhiteSpace(dto.AuthenticityCertificateUrl))
                throw new ArgumentException("Certificate image is required.");

            if (!string.Equals(dto.SubVertical, "Collectibles", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This endpoint only supports posting ads under the 'Collectibles' subvertical.");

            try
            {
                dto.Id = Guid.NewGuid();

                _log.LogTrace($"Calling internal collectibles service with {dto.Images.Count} images and cert: {dto.AuthenticityCertificateUrl}");
                var requestUrl = $"api/classifieds/collectibles/post-by-id";
                var payload = JsonSerializer.Serialize(dto);
                var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, SERVICE_APP_ID, requestUrl);
                req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var res = await _dapr.InvokeMethodWithResponseAsync(req, cancellationToken);
                var body = await res.Content.ReadAsStringAsync(cancellationToken);
                if (!res.IsSuccessStatusCode)
                    throw new DaprServiceException((int)res.StatusCode, body);

                return new AdCreatedResponseDto
                {
                    AdId = dto.Id,
                    Title = dto.Title,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Collectibles Ad created successfully"
                };
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<AdCreatedResponseDto> CreateClassifiedDealsAd(ClassifiedsDeals dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (dto.UserId == null) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (string.IsNullOrWhiteSpace(dto.ImageUrl)) throw new ArgumentException("Ad image is required.");
            if (string.IsNullOrWhiteSpace(dto.FlyerFileUrl)) throw new ArgumentException("FlyerFile image is required.");

            if (!string.Equals(dto.Subvertical, "Deals", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This endpoint only supports posting ads under the 'Deals' subvertical.");

            try
            {
                dto.Id = Guid.NewGuid();
                _log.LogTrace($"Calling internal deals service with flyer: {dto.FlyerFileUrl} and image: {dto.ImageUrl}");
                var requestUrl = $"api/classifieds/deals/post-by-id";
                var payload = JsonSerializer.Serialize(dto);
                var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, SERVICE_APP_ID, requestUrl);
                req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var res = await _dapr.InvokeMethodWithResponseAsync(req, cancellationToken);
                var body = await res.Content.ReadAsStringAsync(cancellationToken);
                if (!res.IsSuccessStatusCode)
                    throw new DaprServiceException((int)res.StatusCode, body);

                return new AdCreatedResponseDto
                {
                    AdId = dto.Id,
                    Title = dto.Title,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Deals Ad created successfully"
                };
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<DeleteAdResponseDto> DeleteClassifiedItemsAd(Guid adId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (adId == Guid.Empty)
                    throw new ArgumentException("Ad ID is required.");

                List<string> blobNamesToDelete = new();

                var response = await _dapr.InvokeMethodAsync<DeleteAdResponseDto>(
                    HttpMethod.Delete,
                    SERVICE_APP_ID,
                    $"api/classifieds/items/{adId}",
                    cancellationToken
                    );


                if (response?.DeletedImages?.Count > 0)
                {
                    foreach (var blobName in response.DeletedImages)
                    {
                        await _fileStorageBlob.DeleteFile(blobName, "classifieds-images", cancellationToken);
                    }

                    _log.LogException(new Exception($"Deleted blobs for Ad ID: {adId}"));
                }
                await _searchService.DeleteAsync(ConstantValues.IndexNames.ClassifiedsItemsIndex, adId.ToString());
                return response;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to delete Classified Items Ad.", ex);
            }
        }

        public async Task<DeleteAdResponseDto> DeleteClassifiedPrelovedAd(Guid adId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (adId == Guid.Empty)
                    throw new ArgumentException("Ad ID is required.");

                List<string> blobNamesToDelete = new();

                var response = await _dapr.InvokeMethodAsync<DeleteAdResponseDto>(
                    HttpMethod.Delete,
                    SERVICE_APP_ID,
                    $"api/classifieds/preloved/{adId}",
                    cancellationToken
                );

                if (response?.DeletedImages?.Count > 0)
                {
                    foreach (var blobName in response.DeletedImages)
                    {
                        await _fileStorageBlob.DeleteFile(blobName, "classifieds-images", cancellationToken);
                    }

                    _log.LogException(new Exception($"Deleted blobs for Preloved Ad ID: {adId}"));
                }
                await _searchService.DeleteAsync(ConstantValues.IndexNames.ClassifiedsPrelovedIndex, adId.ToString());
                return response;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to delete Classified Preloved Ad.", ex);
            }
        }

        public async Task<DeleteAdResponseDto> DeleteClassifiedCollectiblesAd(Guid adId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (adId == Guid.Empty)
                    throw new ArgumentException("Ad ID is required.");

                List<string> blobNamesToDelete = new();

                var response = await _dapr.InvokeMethodAsync<DeleteAdResponseDto>(
                    HttpMethod.Delete,
                    SERVICE_APP_ID,
                    $"api/classifieds/collectibles/{adId}",
                    cancellationToken
                );

                if (response?.DeletedImages?.Count > 0)
                {
                    foreach (var blobName in response.DeletedImages)
                    {
                        await _fileStorageBlob.DeleteFile(blobName, "classifieds-images", cancellationToken);
                    }

                    _log.LogException(new Exception($"Deleted blobs for Collectibles Ad ID: {adId}"));
                }
                await _searchService.DeleteAsync(ConstantValues.IndexNames.ClassifiedsCollectiblesIndex, adId.ToString());
                return response;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to delete Classified Collectibles Ad.", ex);
            }
        }

        public async Task<DeleteAdResponseDto> DeleteClassifiedDealsAd(Guid adId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (adId == Guid.Empty)
                    throw new ArgumentException("Ad ID is required.");

                List<string> blobNamesToDelete = new();

                var response = await _dapr.InvokeMethodAsync<DeleteAdResponseDto>(
                    HttpMethod.Delete,
                    SERVICE_APP_ID,
                    $"api/classifieds/deals/{adId}",
                    cancellationToken
                );

                if (response?.DeletedImages?.Count > 0)
                {
                    foreach (var blobName in response.DeletedImages)
                    {
                        await _fileStorageBlob.DeleteFile(blobName, "classifieds-images", cancellationToken);
                    }

                    _log.LogException(new Exception($"Deleted blobs for Deals Ad ID: {adId}"));
                }
                await _searchService.DeleteAsync(ConstantValues.IndexNames.ClassifiedsDealsIndex, adId.ToString());
                return response;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to delete Classified Deals Ad.", ex);
            }
        }

        public async Task<ClassifiedsItems> GetItemAdById(Guid adId, CancellationToken cancellationToken = default)
        {
            if (adId == Guid.Empty)
                throw new ArgumentException("Ad ID must not be empty.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<ClassifiedsItems>(
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

        public async Task<ClassifiedsPreloved> GetPrelovedAdById(Guid adId, CancellationToken cancellationToken = default)
        {
            if (adId == Guid.Empty)
                throw new ArgumentException("Ad ID must not be empty.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<ClassifiedsPreloved>(
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

        public async Task<ClassifiedsDeals> GetDealsAdById(Guid adId, CancellationToken cancellationToken = default)
        {
            if (adId == Guid.Empty)
                throw new ArgumentException("Ad ID must not be empty.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<ClassifiedsDeals>(
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

        public async Task<ClassifiedsCollectibles> GetCollectiblesAdById(Guid adId, CancellationToken cancellationToken = default)
        {
            if (adId == Guid.Empty)
                throw new ArgumentException("Ad ID must not be empty.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<ClassifiedsCollectibles>(
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

        public async Task<AdUpdatedResponseDto> UpdateClassifiedItemsAd(ClassifiedsItems dto, CancellationToken cancellationToken = default)
        {
            if (dto.Id == null)
                throw new ArgumentException("Id must be specified.", nameof(dto.Id));

            try
            {

                var response = await _dapr.InvokeMethodAsync<ClassifiedsItems, AdUpdatedResponseDto>(
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

        public async Task<AdUpdatedResponseDto> UpdateClassifiedPrelovedAd(ClassifiedsPreloved dto, CancellationToken cancellationToken = default)
        {
            if (dto.Id == null)
            {
                throw new ArgumentException("Id must be specified.");
            }
            try
            {
                var response = await _dapr.InvokeMethodAsync<ClassifiedsPreloved, AdUpdatedResponseDto>(
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

        public async Task<AdUpdatedResponseDto> UpdateClassifiedCollectiblesAd(ClassifiedsCollectibles dto, CancellationToken cancellationToken = default)
        {
            if (dto.Id == null)
            {
                throw new ArgumentException("Id must be specified.");
            }
            try
            {
                var response = await _dapr.InvokeMethodAsync<ClassifiedsCollectibles, AdUpdatedResponseDto>(
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

        public async Task<AdUpdatedResponseDto> UpdateClassifiedDealsAd(ClassifiedsDeals dto, CancellationToken cancellationToken = default)
        {
            if (dto.Id == null)
            {
                throw new ArgumentException("Id must be specified.");
            }

            try
            {
                var response = await _dapr.InvokeMethodAsync<ClassifiedsDeals, AdUpdatedResponseDto>(
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

        public async Task<string> PromoteClassifiedAd(ClassifiedsPromoteDto dto, string userId, CancellationToken cancellationToken = default)
        {
            if (dto.AdId == Guid.Empty)
            {
                throw new ArgumentException("AdId is required.");
            }
            HttpStatusCode? failedStatusCode = null;
            string failedErrorMessage = null;
            try
            {
                var url = $"/api/classifieds/items/promoted/{dto.AdId}?subVertical={dto.SubVertical}";
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
                    failedErrorMessage = errorMessage;
                    throw new InvalidDataException(errorMessage);
                }
                response.EnsureSuccessStatusCode();
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
                throw new InvalidOperationException("Failed to refresh the ad due to an unexpected error.", ex);
            }
        }

        public async Task<string> FeatureClassifiedAd(ClassifiedsPromoteDto dto, string userId, CancellationToken cancellationToken = default)
        {
            if (dto.AdId == Guid.Empty)
            {
                throw new ArgumentException("AdId is required.");
            }
            HttpStatusCode? failedStatusCode = null;
            string failedErrorMessage = null;
            try
            {
                var url = $"/api/classifieds/items/featured/{dto.AdId}?subVertical={dto.SubVertical}";
                var serviceRequest = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, SERVICE_APP_ID, url);
                serviceRequest.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }
                    failedStatusCode = response.StatusCode;
                    failedErrorMessage = errorMessage;
                    throw new InvalidDataException(errorMessage);
                }
                response.EnsureSuccessStatusCode();
                return "The ad has been successfully marked as featured.";
            }
            catch (ArgumentException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("AdId is required and must be valid.", ex);
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
        public async Task<PaginatedAdResponseDto> GetFilteredAds(string subVertical,bool? isPublished,int page,int pageSize,string? search,string userId,CancellationToken cancellationToken = default)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(userId))
                        throw new ArgumentException("User ID must not be empty.", nameof(userId));

                    var queryParams = new Dictionary<string, string>
                    {
                        { "subVertical", subVertical },
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
        public async Task<BulkAdActionResponse> BulkUpdateAdPublishStatusAsync( string subVertical, string userId, List<Guid> adIds, bool isPublished, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subVertical) || string.IsNullOrWhiteSpace(userId) || adIds == null || adIds.Count == 0)
                throw new ArgumentException("Invalid bulk publish/unpublish request.");

            try
            {
                var lowerSubVertical = subVertical.ToLowerInvariant();

                var route = $"api/classifieds/user-dashboard/bulk-action-by-id?subVertical={Uri.EscapeDataString(lowerSubVertical)}&isPublished={isPublished.ToString().ToLowerInvariant()}&userId={Uri.EscapeDataString(userId)}";

                var result = await _dapr.InvokeMethodAsync<List<Guid>, BulkAdActionResponse>(
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
                throw new InvalidOperationException("Failed to update ad publish status from classified microservice.", ex);
            }
        }
    }
}

