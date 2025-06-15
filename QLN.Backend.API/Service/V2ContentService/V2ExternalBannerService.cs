using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.IService.IFileStorage;

namespace QLN.Backend.API.Service.V2ContentService
{
    public class V2ExternalBannerService : IV2contentBannerService
    {
        private readonly DaprClient _daprClient;
        private readonly IFileStorageBlobService _fileStorageBlob;
        private const string InternalAppId = "qln-content-ms";
        private const string BlobContainer = "classifieds-images";

        public V2ExternalBannerService(DaprClient daprClient, IFileStorageBlobService fileStorageBlob)
        {
            _daprClient = daprClient;
            _fileStorageBlob = fileStorageBlob;
        }

        public async Task<BannerResponse> SaveBannerAsync(BannerCreateRequest dto, string userId, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(dto.ImageDesktopBase64))
            {
                var (desktopExt, desktopBase64) = Base64ImageHelper.ParseBase64Image(dto.ImageDesktopBase64);
                var desktopName = $"{dto.Code}_desktop.{desktopExt}";
                var desktopUrl = await _fileStorageBlob.SaveBase64File(desktopBase64, desktopName, BlobContainer, ct);
                dto.ImageDesktopUrl = desktopUrl;
                dto.ImageDesktopBase64 = null;
            }

            if (!string.IsNullOrEmpty(dto.ImageMobileBase64))
            {
                var (mobileExt, mobileBase64) = Base64ImageHelper.ParseBase64Image(dto.ImageMobileBase64);
                var mobileName = $"{dto.Code}_mobile.{mobileExt}";
                var mobileUrl = await _fileStorageBlob.SaveBase64File(mobileBase64, mobileName, BlobContainer, ct);
                dto.ImageMobileUrl = mobileUrl;
                dto.ImageMobileBase64 = null;
            }

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

        public async Task<BannerResponse> UpdateBannerAsync(BannerUpdateRequest dto, string userId, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(dto.ImageDesktopBase64))
            {
                var (desktopExt, desktopBase64) = Base64ImageHelper.ParseBase64Image(dto.ImageDesktopBase64);
                var desktopName = $"{dto.Code}_desktop.{desktopExt}";
                var desktopUrl = await _fileStorageBlob.SaveBase64File(desktopBase64, desktopName, BlobContainer, ct);
                dto.ImageDesktopBase64 = desktopUrl;
            }

            if (!string.IsNullOrEmpty(dto.ImageMobileBase64))
            {
                var (mobileExt, mobileBase64) = Base64ImageHelper.ParseBase64Image(dto.ImageMobileBase64);
                var mobileName = $"{dto.Code}_mobile.{mobileExt}";
                var mobileUrl = await _fileStorageBlob.SaveBase64File(mobileBase64, mobileName, BlobContainer, ct);
                dto.ImageMobileBase64 = mobileUrl;
            }

            return await _daprClient.InvokeMethodAsync<BannerUpdateRequest, BannerResponse>(
                HttpMethod.Put,
                InternalAppId,
                $"api/v2/content/banner/update/{dto.Category}/{dto.Code}",
                dto,
                ct);
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

                var request = _daprClient.CreateInvokeMethodRequest(
                    HttpMethod.Delete,
                    InternalAppId,
                    endpoint);

                var response = await _daprClient.InvokeMethodWithResponseAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return false;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync(ct);
                    throw new Exception($"Dapr delete failed with status {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                throw;
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
