using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Utilities;

public class V2ExternalBannerService : IV2contentBannerService
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<V2ExternalBannerService> _logger;
    private readonly IFileStorageBlobService _fileStorageBlob;

    private const string InternalAppId = "qln-content-ms";
    private const string BlobContainer = "classifieds-images";

    public V2ExternalBannerService(DaprClient daprClient, ILogger<V2ExternalBannerService> logger, IFileStorageBlobService fileStorageBlob)
    {
        _daprClient = daprClient;
        _logger = logger;
        _fileStorageBlob = fileStorageBlob;
    }

    public async Task<BannerResponse> SaveBannerAsync(BannerCreateRequest dto, string userId, CancellationToken ct = default)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID is required", nameof(userId));

        dto.CreatedBy = userId;

        // Upload images if present (dynamic extension logic)
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

        // Now call internal (QLN.Content.MS) via Dapr, only with URLs
        var result = await _daprClient.InvokeMethodAsync<BannerCreateRequest, BannerResponse>(
            HttpMethod.Post,
            InternalAppId,
            "api/v2/content/banner",
            dto,
            ct
        );

        return result;
    }

    public async Task<List<BannerItem>> GetBannersByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Category is required", nameof(category));
        var result = await _daprClient.InvokeMethodAsync<List<BannerItem>>(
            HttpMethod.Get,
            InternalAppId,
            $"api/v2/content/banner/{category}",
            cancellationToken
        );
        return result ?? new List<BannerItem>();
    }
}
