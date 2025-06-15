using Dapr.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Utilities;
using System.Net;
using System.Text;
using System.Text.Json;

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

        public async Task<BannerResponse> SaveBannerAsync(BannerCreateRequest dto, string userId = null, CancellationToken ct = default)
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

        public async Task<List<BannerItem>> GetBannersByCategoryAsync(string category, CancellationToken ct = default)
        {
            return await _daprClient.InvokeMethodAsync<List<BannerItem>>(
                HttpMethod.Get,
                InternalAppId,
                $"api/v2/content/banner/{category}",
                ct);
        }

        public async Task<BannerResponse> UpdateBannerAsync(BannerUpdateRequest dto, string userId = null, CancellationToken ct = default)
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

        public async Task<bool> DeleteBannerFromStateAsync(string category, string code, CancellationToken ct = default)
        {
            var endpoint = $"api/v2/content/banner/delete-state/{category}/{code}";

            var request = _daprClient.CreateInvokeMethodRequest(HttpMethod.Delete, InternalAppId, endpoint);

            var response = await _daprClient.InvokeMethodWithResponseAsync(request, ct);

            if (response.IsSuccessStatusCode)
                return true;
            else if (response.StatusCode == HttpStatusCode.NotFound)
                return false;
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Delete failed: {Error}", error);
                throw new Exception($"Dapr delete failed with status {response.StatusCode}");
            }
        }

        public async Task<Dictionary<string, BaseQueueResponse<BannerItem>>> GetAllBannersAsync(CancellationToken ct = default)
        {
            return await _daprClient.InvokeMethodAsync<Dictionary<string, BaseQueueResponse<BannerItem>>>(
                HttpMethod.Get,
                InternalAppId,
                "api/v2/content/banner/all",
                ct);
        }
    }
}
