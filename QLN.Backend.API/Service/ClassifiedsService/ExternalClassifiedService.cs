using Dapr;
using Dapr.Client;
using Microsoft.Spatial;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Utilities;
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


        public async Task<ItemAdsAndDashboardResponse> GetUserItemsAdsWithDashboard(string userId, CancellationToken cancellationToken = default)
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

        public async Task<PrelovedAdsAndDashboardResponse> GetUserPrelovedAdsAndDashboard(string userId, CancellationToken cancellationToken = default)
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
            if (string.IsNullOrWhiteSpace(dto.AuthenticityCertificateUrl))
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


        public async Task<CollectiblesResponse> GetCollectibles(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID is required", nameof(userId));

                _log.LogException(new Exception($"Starting to fetch collectibles for userId: {userId}"));

                var result = await _dapr.InvokeMethodAsync<CollectiblesResponse>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{Vertical}/collectibles-by-id?userId={userId}",
                    cancellationToken
                );

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
                    $"api/classifieds/items-ad/{adId}",
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
                    $"api/classifieds/preloved-ad/{adId}",
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
                    $"api/classifieds/collectibles-ad/{adId}",
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
                    $"api/classifieds/deals-ad/{adId}",
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
                    $"api/classifieds/items/ads/{adId}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException($"Failed to retrieve ad details for Ad ID: {adId} from classified microservice.", ex);
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
                    $"api/classifieds/preloved/ad/{adId}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve Preloved ad from classified microservice.", ex);
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
                    $"api/classifieds/deals/ad/{adId}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve Deals ad from classified microservice.", ex);
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
                    $"api/classifieds/collectibles/ad/{adId}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve Collectibles ad from classified microservice.", ex);
            }
        }

        public async Task<PaginatedAdResponseDto> GetUserPublishedItemsAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            if (userId == null)
                throw new ArgumentException("User ID must not be empty.");

            try
            {
                var queryParams = new Dictionary<string, string>();

                if (page.HasValue)
                    queryParams.Add("page", page.Value.ToString());

                if (pageSize.HasValue)
                    queryParams.Add("pageSize", pageSize.Value.ToString());

                if (sortOption.HasValue)
                    queryParams.Add("sortOption", ((int)sortOption.Value).ToString());

                if (!string.IsNullOrWhiteSpace(search))
                    queryParams.Add("search", search);

                var queryString = queryParams.Count > 0
                    ? "?" + string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                    : string.Empty;

                var result = await _dapr.InvokeMethodAsync<PaginatedAdResponseDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/items/user-ads-by-id/{userId}/published{queryString}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve user's item ads from classified microservice.", ex);
            }
        }

        public async Task<PaginatedAdResponseDto> GetUserUnPublishedItemsAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            if (userId == null)
                throw new ArgumentException("User ID must not be empty.");
            try
            {
                var queryParams = new Dictionary<string, string>();

                if (page.HasValue)
                    queryParams.Add("page", page.Value.ToString());

                if (pageSize.HasValue)
                    queryParams.Add("pageSize", pageSize.Value.ToString());

                if (sortOption.HasValue)
                    queryParams.Add("sortOption", ((int)sortOption.Value).ToString());

                if (!string.IsNullOrWhiteSpace(search))
                    queryParams.Add("search", search);

                var queryString = queryParams.Count > 0
                    ? "?" + string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                    : string.Empty;
                var result = await _dapr.InvokeMethodAsync<PaginatedAdResponseDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/items/user-ads-by-id/{userId}/unpublished{queryString}",
                    cancellationToken);
                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve user's unpublished item ads from classified microservice.", ex);
            }
        }

        public async Task<PaginatedPrelovedAdResponseDto> GetUserPublishedPrelovedAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            if (userId == null)
                throw new ArgumentException("User ID must not be empty.");

            try
            {
                var queryParams = new Dictionary<string, string>();

                if (page.HasValue)
                    queryParams.Add("page", page.Value.ToString());

                if (pageSize.HasValue)
                    queryParams.Add("pageSize", pageSize.Value.ToString());


                if (sortOption.HasValue)
                    queryParams.Add("sortOption", ((int)sortOption.Value).ToString());

                if (!string.IsNullOrWhiteSpace(search))
                    queryParams.Add("search", search);

                var queryString = queryParams.Count > 0
                    ? "?" + string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                    : string.Empty;

                var result = await _dapr.InvokeMethodAsync<PaginatedPrelovedAdResponseDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/preloved/user-ads-by-id/{userId}/published{queryString}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve user's published preloved ads from classified microservice.", ex);
            }
        }

        public async Task<PaginatedPrelovedAdResponseDto> GetUserUnPublishedPrelovedAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            if (userId == null)
                throw new ArgumentException("User ID must not be empty.");

            try
            {
                var queryParams = new Dictionary<string, string>();

                if (page.HasValue)
                    queryParams.Add("page", page.Value.ToString());

                if (pageSize.HasValue)
                    queryParams.Add("pageSize", pageSize.Value.ToString());

                if (sortOption.HasValue)
                    queryParams.Add("sortOption", ((int)sortOption.Value).ToString());

                if (!string.IsNullOrWhiteSpace(search))
                    queryParams.Add("search", search);

                var queryString = queryParams.Count > 0
                    ? "?" + string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                    : string.Empty;

                var result = await _dapr.InvokeMethodAsync<PaginatedPrelovedAdResponseDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/preloved/user-ads-by-id/{userId}/unpublished{queryString}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve user's unpublished preloved ads from classified microservice.", ex);
            }
        }

        public async Task<PaginatedCollectiblesAdResponseDto> GetUserPublishedCollectiblesAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            if (userId == null)
                throw new ArgumentException("User ID must not be empty.", nameof(userId));

            try
            {
                var queryParams = new Dictionary<string, string>();
                if (page.HasValue) queryParams["page"] = page.Value.ToString();
                if (pageSize.HasValue) queryParams["pageSize"] = pageSize.Value.ToString();

                if (sortOption.HasValue)
                    queryParams.Add("sortOption", ((int)sortOption.Value).ToString());

                if (!string.IsNullOrWhiteSpace(search))
                    queryParams.Add("search", search);

                var queryString = queryParams.Count > 0
                    ? "?" + string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                    : string.Empty;

                return await _dapr.InvokeMethodAsync<PaginatedCollectiblesAdResponseDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/collectibles/user-ads-by-id/{userId}/published{queryString}",
                    cancellationToken);
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException(
                    "Failed to retrieve user's published collectibles ads from classified microservice.", ex);
            }
        }

        public async Task<PaginatedCollectiblesAdResponseDto> GetUserUnPublishedCollectiblesAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            if (userId == null)
                throw new ArgumentException("User ID must not be empty.", nameof(userId));

            try
            {
                var queryParams = new Dictionary<string, string>();
                if (page.HasValue) queryParams["page"] = page.Value.ToString();
                if (pageSize.HasValue) queryParams["pageSize"] = pageSize.Value.ToString();

                if (sortOption.HasValue)
                    queryParams.Add("sortOption", ((int)sortOption.Value).ToString());

                if (!string.IsNullOrWhiteSpace(search))
                    queryParams.Add("search", search);

                var queryString = queryParams.Count > 0
                    ? "?" + string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                    : string.Empty;

                return await _dapr.InvokeMethodAsync<PaginatedCollectiblesAdResponseDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/collectibles/user-ads-by-id/{userId}/unpublished{queryString}",
                    cancellationToken);
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException(
                    "Failed to retrieve user's unpublished collectibles ads from classified microservice.", ex);
            }
        }

        public async Task<PaginatedDealsAdResponseDto> GetUserPublishedDealsAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            if (userId == null)
                throw new ArgumentException("User ID must not be empty.", nameof(userId));

            try
            {
                var queryParams = new Dictionary<string, string>();
                if (page.HasValue) queryParams["page"] = page.Value.ToString();
                if (pageSize.HasValue) queryParams["pageSize"] = pageSize.Value.ToString();

                if (sortOption.HasValue)
                    queryParams.Add("sortOption", ((int)sortOption.Value).ToString());

                if (!string.IsNullOrWhiteSpace(search))
                    queryParams.Add("search", search);

                var queryString = queryParams.Count > 0
                    ? "?" + string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                    : string.Empty;

                return await _dapr.InvokeMethodAsync<PaginatedDealsAdResponseDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/deals/user-ads-by-id/{userId}/published{queryString}",
                    cancellationToken);
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException(
                    "Failed to retrieve user's published deals ads from classified microservice.",
                    ex);
            }
        }

        public async Task<PaginatedDealsAdResponseDto> GetUserUnPublishedDealsAds(string userId, int? page, int? pageSize, AdSortOption? sortOption = null, string? search = null, CancellationToken cancellationToken = default)
        {
            if (userId == null)
                throw new ArgumentException("User ID must not be empty.", nameof(userId));

            try
            {
                var queryParams = new Dictionary<string, string>();
                if (page.HasValue) queryParams["page"] = page.Value.ToString();
                if (pageSize.HasValue) queryParams["pageSize"] = pageSize.Value.ToString();

                if (sortOption.HasValue)
                    queryParams.Add("sortOption", ((int)sortOption.Value).ToString());

                if (!string.IsNullOrWhiteSpace(search))
                    queryParams.Add("search", search);

                var queryString = queryParams.Count > 0
                    ? "?" + string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                    : string.Empty;

                return await _dapr.InvokeMethodAsync<PaginatedDealsAdResponseDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/deals/user-ads-by-id/{userId}/unpublished{queryString}",
                    cancellationToken);
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException(
                    "Failed to retrieve user's unpublished deals ads from classified microservice.",
                    ex);
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

        public async Task<BulkAdActionResponse> BulkUnpublishItemsAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            if (userId == null || adIds == null || adIds.Count == 0)
                throw new ArgumentException("Invalid bulk publish request.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<List<Guid>, BulkAdActionResponse>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/classifieds/items/user-ads-by-id/{userId}/unpublish",
                    adIds,
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to unpublish ads from classified microservice.", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkPublishItemsAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            if (userId == null || adIds == null || adIds.Count == 0)
                throw new ArgumentException("Invalid bulk publish request.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<List<Guid>, BulkAdActionResponse>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/classifieds/items/user-ads-by-id/{userId}/publish",
                    adIds,
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to publish ads to classified microservice.", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkPublishPrelovedAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            if (userId == null || adIds == null || adIds.Count == 0)
                throw new ArgumentException("Invalid bulk publish request.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<List<Guid>, BulkAdActionResponse>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/classifieds/preloved/user-ads-by-id/{userId}/publish",
                    adIds,
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to publish preloved ads to classified microservice.", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkUnpublishPrelovedAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            if (userId == null || adIds == null || adIds.Count == 0)
                throw new ArgumentException("Invalid bulk unpublish request.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<List<Guid>, BulkAdActionResponse>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/classifieds/preloved/user-ads-by-id/{userId}/unpublish",
                    adIds,
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to unpublish preloved ads from classified microservice.", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkPublishDealsAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            if (userId == null || adIds == null || adIds.Count == 0)
                throw new ArgumentException("Invalid bulk publish request.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<List<Guid>, BulkAdActionResponse>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/classifieds/deals/user-ads-by-id/{userId}/publish",
                    adIds,
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to publish deals ads to classified microservice.", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkUnpublishDealsAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            if (userId == null || adIds == null || adIds.Count == 0)
                throw new ArgumentException("Invalid bulk unpublish request.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<List<Guid>, BulkAdActionResponse>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/classifieds/deals/user-ads-by-id/{userId}/unpublish",
                    adIds,
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to unpublish deals ads from classified microservice.", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkPublishCollectiblesAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            if (userId == null || adIds == null || adIds.Count == 0)
                throw new ArgumentException("Invalid bulk publish request.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<List<Guid>, BulkAdActionResponse>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/classifieds/collectibles/user-ads-by-id/{userId}/publish",
                    adIds,
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to publish collectibles ads to classified microservice.", ex);
            }
        }

        public async Task<BulkAdActionResponse> BulkUnpublishCollectiblesAds(string userId, List<Guid> adIds, CancellationToken cancellationToken = default)
        {
            if (userId == null || adIds == null || adIds.Count == 0)
                throw new ArgumentException("Invalid bulk unpublish request.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<List<Guid>, BulkAdActionResponse>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/classifieds/collectibles/user-ads-by-id/{userId}/unpublish",
                    adIds,
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to unpublish collectibles ads from classified microservice.", ex);
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
                _log.LogException(ex);
                throw new InvalidOperationException(
                    "Failed to update classified item ad in the classifieds microservice.",
                    ex);
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
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to update classified preloved ad in the classifieds microservice.", ex);
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
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to update classified collectibles ad in the classifieds microservice.", ex);
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
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to update classified deals ad in the classifieds microservice.", ex);
            }
        }
    }
}

