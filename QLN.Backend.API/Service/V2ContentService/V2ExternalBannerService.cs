using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Utilities;
using System.Net;

namespace QLN.Backend.API.Service.V2ContentService
{
    public class V2ExternalBannerService : IV2contentBannerService
    {
        private readonly DaprClient _daprClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileStorageBlobService _fileStorageBlob;
        private readonly ILogger<V2ExternalBannerService> _logger;
        private const string InternalAppId = "qln-content-ms";
        private const string BlobContainer = "classifieds-images";

        public V2ExternalBannerService(
            DaprClient daprClient,
            IHttpContextAccessor httpContextAccessor,
            IFileStorageBlobService fileStorageBlob,
            ILogger<V2ExternalBannerService> logger)
        {
            _daprClient = daprClient;
            _httpContextAccessor = httpContextAccessor;
            _fileStorageBlob = fileStorageBlob;
            _logger = logger;
        }

        private string GetUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User.GetId();
            return userId?.ToString() ?? throw new UnauthorizedAccessException("User ID not found in token.");
        }

        public async Task<BannerResponse> SaveBannerAsync(BannerCreateRequest dto, string? userId = null, CancellationToken ct = default)
        {
            try
            {
                var resolvedUserId = userId ?? GetUserId();

                if (!string.IsNullOrEmpty(dto.ImageDesktopBase64))
                {
                    var (ext, base64) = Base64ImageHelper.ParseBase64Image(dto.ImageDesktopBase64);
                    var fileName = $"{dto.Code}_desktop.{ext}";
                    dto.ImageDesktopUrl = await _fileStorageBlob.SaveBase64File(base64, fileName, BlobContainer, ct);
                    dto.ImageDesktopBase64 = null;
                }

                if (!string.IsNullOrEmpty(dto.ImageMobileBase64))
                {
                    var (ext, base64) = Base64ImageHelper.ParseBase64Image(dto.ImageMobileBase64);
                    var fileName = $"{dto.Code}_mobile.{ext}";
                    dto.ImageMobileUrl = await _fileStorageBlob.SaveBase64File(base64, fileName, BlobContainer, ct);
                    dto.ImageMobileBase64 = null;
                }

                dto.CreatedBy = resolvedUserId;

                return await _daprClient.InvokeMethodAsync<BannerCreateRequest, BannerResponse>(
                    HttpMethod.Post,
                    InternalAppId,
                    "api/v2/content/banner",
                    dto,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while saving banner for category {Category}", dto.Category);
                throw ;
            }
        }

        public async Task<List<BannerItem>> GetBannersByCategoryAsync(string category, CancellationToken ct = default)
        {
            try
            {
                return await _daprClient.InvokeMethodAsync<List<BannerItem>>(
                    HttpMethod.Get,
                    InternalAppId,
                    $"api/v2/content/banner/{category}",
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching banners for category {Category}", category);
                throw ;
            }
        }

        public async Task<BannerResponse> UpdateBannerAsync(BannerUpdateRequest dto, string? userId = null, CancellationToken ct = default)
        {
            try
            {
                var resolvedUserId = userId ?? GetUserId();

                if (!string.IsNullOrEmpty(dto.ImageDesktopBase64))
                {
                    var (ext, base64) = Base64ImageHelper.ParseBase64Image(dto.ImageDesktopBase64);
                    var fileName = $"{dto.Code}_desktop.{ext}";
                    dto.ImageDesktopBase64 = await _fileStorageBlob.SaveBase64File(base64, fileName, BlobContainer, ct);
                }

                if (!string.IsNullOrEmpty(dto.ImageMobileBase64))
                {
                    var (ext, base64) = Base64ImageHelper.ParseBase64Image(dto.ImageMobileBase64);
                    var fileName = $"{dto.Code}_mobile.{ext}";
                    dto.ImageMobileBase64 = await _fileStorageBlob.SaveBase64File(base64, fileName, BlobContainer, ct);
                }

                dto.UpdatedBy = resolvedUserId;

                return await _daprClient.InvokeMethodAsync<BannerUpdateRequest, BannerResponse>(
                    HttpMethod.Put,
                    InternalAppId,
                    $"api/v2/content/banner/update/{dto.Category}/{dto.Code}",
                    dto,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating banner {Category} - {Code}", dto.Category, dto.Code);
                throw ;
            }
        }

        public async Task<bool> DeleteBannerFromStateAsync(string category, string code, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category))
                    throw new ArgumentException("Category is required.", nameof(category));
                if (string.IsNullOrWhiteSpace(code))
                    throw new ArgumentException("Code is required.", nameof(code));

                var endpoint = $"api/v2/content/banner/delete-state/{category}/{code}";
                var request = _daprClient.CreateInvokeMethodRequest(HttpMethod.Delete, InternalAppId, endpoint);

                var response = await _daprClient.InvokeMethodWithResponseAsync(request, ct);

                if (response.IsSuccessStatusCode)
                    return true;
                else if (response.StatusCode == HttpStatusCode.NotFound)
                    return false;

                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Delete failed: {Error}", error);
                throw new ApplicationException($"Delete failed with status code {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting banner {Category} - {Code}", category, code);
                throw ;
            }
        }

        public async Task<Dictionary<string, BaseQueueResponse<BannerItem>>> GetAllBannersAsync(CancellationToken ct = default)
        {
            try
            {
                return await _daprClient.InvokeMethodAsync<Dictionary<string, BaseQueueResponse<BannerItem>>>(
                    HttpMethod.Get,
                    InternalAppId,
                    "api/v2/content/banner/all",
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all banners.");
                throw;
            }
        }
    }
}
