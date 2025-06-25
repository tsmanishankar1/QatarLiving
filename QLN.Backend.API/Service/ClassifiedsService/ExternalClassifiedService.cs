using Dapr;
using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Utilities;

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

        public ExternalClassifiedService(DaprClient dapr, IEventlogger log, IHttpContextAccessor httpContextAccessor, IFileStorageBlobService fileStorageBlob)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _httpContextAccessor = httpContextAccessor;
            _fileStorageBlob = fileStorageBlob;
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

        public async Task<bool> SaveSearch(SaveSearchRequestDto dto, Guid userId, CancellationToken cancellationToken = default)
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

        public async Task<AdCreatedResponseDto> CreateClassifiedItemsAd(ClassifiedItems dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (dto.UserId == Guid.Empty) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (dto.AdImagesBase64 == null || dto.AdImagesBase64.Count == 0)
                throw new ArgumentException("At least one ad image is required.");
            if (string.IsNullOrWhiteSpace(dto.CertificateBase64))
                throw new ArgumentException("Certificate image is required.");

            if (!string.Equals(dto.SubVertical, "Items", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This endpoint only supports posting ads under the 'Items' subvertical.");

            var uploadedBlobKeys = new List<string>();

            try
            {
                var adId = Guid.NewGuid();
                dto.Id = adId;


                var (certExt, certBase64) = Base64ImageHelper.ParsePdfFile(dto.CertificateBase64);

                // Upload certificate
                var certFileName = !string.IsNullOrWhiteSpace(dto.CertificateFileName)
                    ? dto.CertificateFileName
                    : $"certificate_{dto.UserId}_{adId}.{certExt}";

                var certUrl = await _fileStorageBlob.SaveBase64File(certBase64, certFileName, "classifieds-images", cancellationToken);
                uploadedBlobKeys.Add(certFileName);
                dto.CertificateFileName = certUrl;
                dto.CertificateBase64 = null;

                // Upload images with order
                for (int i = 0; i < dto.AdImagesBase64.Count; i++)
                {
                    var image = dto.AdImagesBase64[i];
                    var (imgExt, base64Image) = Base64ImageHelper.ParseBase64Image(image.Url);

                    var customName = !string.IsNullOrWhiteSpace(image.AdImageFileNames)
                        ? image.AdImageFileNames
                        : $"Itemsad_{dto.UserId}_{adId}_{i + 1}.{imgExt}";

                    var url = await _fileStorageBlob.SaveBase64File(base64Image, customName, "classifieds-images", cancellationToken);
                    uploadedBlobKeys.Add(customName);

                    image.AdImageFileNames = customName;
                    image.Url = url;
                }

                _log.LogTrace($"Calling internal service with {dto.AdImagesBase64.Count} images");

                await _dapr.InvokeMethodAsync(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/classifieds/items/post-by-id",
                    dto,
                    cancellationToken
                );

                return new AdCreatedResponseDto
                {
                    AdId = adId,
                    Title = dto.Title,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Items Ad created successfully"
                };
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                foreach (var blobName in uploadedBlobKeys)
                {
                    try
                    {
                        await _fileStorageBlob.DeleteFile(blobName, "classifieds-images", cancellationToken);
                    }
                    catch (Exception rollbackEx)
                    {
                        _log.LogException(rollbackEx);
                    }
                }

                throw new InvalidOperationException("Ad creation failed after uploading images. All uploaded files have been cleaned up.", ex);
            }
        }

        public async Task<AdCreatedResponseDto> CreateClassifiedPrelovedAd(ClassifiedPreloved dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            if (dto.UserId == Guid.Empty) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (dto.AdImagesBase64 == null || dto.AdImagesBase64.Count == 0)
                throw new ArgumentException("At least one ad image is required.");
            if (string.IsNullOrWhiteSpace(dto.CertificateBase64))
                throw new ArgumentException("Certificate image is required.");

            if (!string.Equals(dto.SubVertical, "Preloved", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This endpoint only supports posting ads under the 'Preloved' subvertical.");

            var uploadedBlobKeys = new List<string>();

            try
            {
                var adId = Guid.NewGuid();
                dto.Id = adId;

                var (certExt, certBase64) = Base64ImageHelper.ParsePdfFile(dto.CertificateBase64);

                var certFileName = !string.IsNullOrWhiteSpace(dto.CertificateFileName)
                    ? dto.CertificateFileName
                    : $"certificate_{dto.UserId}_{adId}.{certExt}";

                var certUrl = await _fileStorageBlob.SaveBase64File(certBase64, certFileName, "classifieds-images", cancellationToken);
                uploadedBlobKeys.Add(certFileName);
                dto.CertificateFileName = certUrl;
                dto.CertificateBase64 = null;

                for (int i = 0; i < dto.AdImagesBase64.Count; i++)
                {
                    var image = dto.AdImagesBase64[i];

                    var (imgExt, base64Image) = Base64ImageHelper.ParseBase64Image(image.Url);

                    var customName = !string.IsNullOrWhiteSpace(image.AdImageFileNames)
                        ? image.AdImageFileNames
                        : $"PrelovedAd_{dto.UserId}_{adId}_{i + 1}.{imgExt}";

                    var url = await _fileStorageBlob.SaveBase64File(base64Image, customName, "classifieds-images", cancellationToken);
                    uploadedBlobKeys.Add(customName);

                    image.AdImageFileNames = customName;
                    image.Url = url;
                }

                _log.LogTrace($"Calling internal service with CertificateUrl: {dto.CertificateFileName} and {dto.AdImagesBase64.Count} images");

                await _dapr.InvokeMethodAsync(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/classifieds/preloved/post-by-id",
                    dto,
                    cancellationToken
                );

                return new AdCreatedResponseDto
                {
                    AdId = adId,
                    Title = dto.Title,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Preloved Ad created successfully"
                };
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                foreach (var blobName in uploadedBlobKeys)
                {
                    try
                    {
                        await _fileStorageBlob.DeleteFile(blobName, "classifieds-images", cancellationToken);
                    }
                    catch (Exception rollbackEx)
                    {
                        _log.LogException(rollbackEx);
                    }
                }

                throw new InvalidOperationException("Ad creation failed after uploading images. All uploaded files have been cleaned up.", ex);
            }
        }

        public async Task<AdCreatedResponseDto> CreateClassifiedCollectiblesAd(ClassifiedCollectibles dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            if (dto.UserId == Guid.Empty) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (dto.AdImagesBase64 == null || dto.AdImagesBase64.Count == 0)
                throw new ArgumentException("At least one ad image is required.");
            if (string.IsNullOrWhiteSpace(dto.CertificateBase64))
                throw new ArgumentException("Certificate image is required.");

            if (!string.Equals(dto.SubVertical, "Collectibles", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This endpoint only supports posting ads under the 'Collectibles' subvertical.");

            var uploadedBlobKeys = new List<string>();

            try
            {
                var adId = Guid.NewGuid();
                dto.Id = adId;


                var (certExt, certBase64) = Base64ImageHelper.ParsePdfFile(dto.CertificateBase64);

                var certFileName = !string.IsNullOrWhiteSpace(dto.CertificateFileName)
                    ? dto.CertificateFileName
                    : $"certificate_{dto.UserId}_{adId}.{certExt}";

                var certUrl = await _fileStorageBlob.SaveBase64File(certBase64, certFileName, "classifieds-images", cancellationToken);
                uploadedBlobKeys.Add(certFileName);
                dto.CertificateFileName = certUrl;
                dto.CertificateBase64 = null;

                for (int i = 0; i < dto.AdImagesBase64.Count; i++)
                {
                    var image = dto.AdImagesBase64[i];
                    var (imgExt, base64Image) = Base64ImageHelper.ParseBase64Image(image.Url);

                    var fileName = !string.IsNullOrWhiteSpace(image.AdImageFileNames)
                        ? image.AdImageFileNames
                        : $"collectibles_ad_{dto.UserId}_{adId}_{i + 1}.{imgExt}";

                    var blobUrl = await _fileStorageBlob.SaveBase64File(base64Image, fileName, "classifieds-images", cancellationToken);
                    uploadedBlobKeys.Add(fileName);

                    image.AdImageFileNames = fileName;
                    image.Url = blobUrl;
                }

                _log.LogTrace($"Calling internal collectibles service with {dto.AdImagesBase64.Count} images and cert: {dto.CertificateFileName}");

                await _dapr.InvokeMethodAsync(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/classifieds/collectibles/post-by-id",
                    dto,
                    cancellationToken
                );

                return new AdCreatedResponseDto
                {
                    AdId = adId,
                    Title = dto.Title,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Collectibles Ad created successfully"
                };
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                foreach (var blobName in uploadedBlobKeys)
                {
                    try
                    {
                        await _fileStorageBlob.DeleteFile(blobName, "classifieds-images", cancellationToken);
                    }
                    catch (Exception rollbackEx)
                    {
                        _log.LogException(rollbackEx);
                    }
                }

                throw new InvalidOperationException("Ad creation failed after uploading images. All uploaded files have been cleaned up.", ex);
            }
        }

        public async Task<AdCreatedResponseDto> CreateClassifiedDealsAd(ClassifiedDeals dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            if (dto.UserId == Guid.Empty) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (dto.AdImagesBase64 == null || dto.AdImagesBase64.Count == 0)
                throw new ArgumentException("At least one ad image is required.");
            if (string.IsNullOrWhiteSpace(dto.FlyerFile))
                throw new ArgumentException("FlyerFile image is required.");

            if (!string.Equals(dto.SubVertical, "Deals", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This endpoint only supports posting ads under the 'Deals' subvertical.");

            var uploadedBlobKeys = new List<string>();

            try
            {
                var adId = Guid.NewGuid();
                dto.Id = adId;

                var (flyerExt, flyerBase64) = Base64ImageHelper.ParsePdfFile(dto.FlyerFile);

                var flyerName = !string.IsNullOrWhiteSpace(dto.FlyerName)
                    ? dto.FlyerName
                    : $"deals_flyer_{dto.UserId}_{adId}.{flyerExt}";

                var flyerUrl = await _fileStorageBlob.SaveBase64File(dto.FlyerFile, flyerName, "classifieds-images", cancellationToken);
                uploadedBlobKeys.Add(flyerName);
                dto.FlyerFile = null;
                dto.FlyerName = flyerUrl;

                for (int i = 0; i < dto.AdImagesBase64.Count; i++)
                {
                    var image = dto.AdImagesBase64[i];

                    var (imgExt, base64Image) = Base64ImageHelper.ParseBase64Image(image.Url);

                    var fileName = !string.IsNullOrWhiteSpace(image.AdImageFileNames)
                        ? image.AdImageFileNames
                        : $"DealsAd_{dto.UserId}_{adId}_{i + 1}.{imgExt}";

                    var url = await _fileStorageBlob.SaveBase64File(base64Image, fileName, "classifieds-images", cancellationToken);
                    uploadedBlobKeys.Add(fileName);

                    image.AdImageFileNames = fileName;
                    image.Url = url;
                }

                _log.LogTrace($"Calling internal deals service with flyer: {dto.FlyerName} and {dto.AdImagesBase64.Count} images");

                await _dapr.InvokeMethodAsync(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"api/classifieds/deals/post-by-id",
                    dto,
                    cancellationToken
                );

                return new AdCreatedResponseDto
                {
                    AdId = adId,
                    Title = dto.Title,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Deals Ad created successfully"
                };
            }
            catch (Exception ex)
            {
                _log.LogException(ex);

                foreach (var blobName in uploadedBlobKeys)
                {
                    try
                    {
                        await _fileStorageBlob.DeleteFile(blobName, "classifieds-images", cancellationToken);
                    }
                    catch (Exception rollbackEx)
                    {
                        _log.LogException(rollbackEx);
                    }
                }

                throw new InvalidOperationException("Ad creation failed after uploading images. All uploaded files have been cleaned up.", ex);
            }
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

                return response;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to delete Classified Deals Ad.", ex);
            }
        }

        public async Task<ItemAdListDto> GetUserItemsAd(Guid userId, CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID must not be empty.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<ItemAdListDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/items/user-ads-by-id/{userId}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve user's item ads from classified microservice.", ex);
            }
        }

        public async Task<PrelovedAdListDto> GetUserPrelovedAds(Guid userId, CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID must not be empty.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<PrelovedAdListDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/preloved/user-ads-by-id/{userId}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve user's preloved ads from classified microservice.", ex);
            }
        }

        public async Task<CollectiblesAdListDto> GetUserCollectiblesAds(Guid userId, CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID must not be empty.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<CollectiblesAdListDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/collectibles/user-ads-by-id/{userId}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve user's collectibles ads from classified microservice.", ex);
            }
        }

        public async Task<DealsAdListDto> GetUserDealsAds(Guid userId, CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID must not be empty.");

            try
            {
                var result = await _dapr.InvokeMethodAsync<DealsAdListDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/classifieds/deals/user-ads-by-id/{userId}",
                    cancellationToken);

                return result;
            }
            catch (InvocationException ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException("Failed to retrieve user's deals ads from classified microservice.", ex);
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

    }
}

