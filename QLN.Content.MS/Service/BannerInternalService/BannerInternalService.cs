using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;

public class BannerService : IV2contentBannerService
{
    private readonly DaprClient _daprClient;
    private const string StateStore = "bannerstatestore";

    public BannerService(DaprClient daprClient) => _daprClient = daprClient;

    private static string GetCategoryKey(string category) => $"banner-{category.ToLower()}";

    public async Task<BannerResponse> SaveBannerAsync(BannerCreateRequest dto, string userId, CancellationToken ct = default)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Category)) throw new ArgumentException("Category is required.", nameof(dto.Category));
        if (string.IsNullOrWhiteSpace(dto.Code)) throw new ArgumentException("Code is required.", nameof(dto.Code));
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID is required.", nameof(userId));

        var key = GetCategoryKey(dto.Category);
        var banners = await _daprClient.GetStateAsync<List<BannerItem>>(StateStore, key, null, null, ct) ?? new();

        // Remove/replace if code exists
        banners.RemoveAll(x => x.Code == dto.Code);
        banners.Add(new BannerItem
        {
            Code = dto.Code,
            Alt = dto.Alt,
            Duration = dto.Duration,
            ImageDesktopUrl = dto.ImageDesktopUrl,
            ImageMobileUrl = dto.ImageMobileUrl,
            Link = dto.Link,
            CreatedBy = userId
        });

        await _daprClient.SaveStateAsync(StateStore, key, banners);

        var response = new BannerResponse
        {
            // Your logic to return the new/updated banners list (example below)
            QlnBannersDailyHero = banners
        };
        return response;
    }

    public async Task<List<BannerItem>> GetBannersByCategoryAsync(string category, CancellationToken ct = default)
    {
        var key = GetCategoryKey(category);
        var banners = await _daprClient.GetStateAsync<List<BannerItem>>(StateStore, key, null, null, ct) ?? new();
        return banners;
    }

    public async Task<BannerResponse> UpdateBannerAsync(BannerUpdateRequest dto, string userId, CancellationToken ct = default)
    {
        var key = GetCategoryKey(dto.Category);
        var banners = await _daprClient.GetStateAsync<List<BannerItem>>(StateStore, key, null, null, ct) ?? new();

        var banner = banners.FirstOrDefault(x => x.Code == dto.Code);
        if (banner == null)
            throw new Exception("Banner not found!");

        // Update the fields as needed
        if (dto.Alt != null) banner.Alt = dto.Alt;
        if (dto.Category != null) banner.Category = dto.Category;
        if (dto.Code != null) banner.Code = dto.Code;
        if (dto.Duration != null) banner.Duration = dto.Duration;
        if (dto.Link != null) banner.Link = dto.Link;
        // Update image URLs if sent
        if (dto.ImageDesktopBase64 != null) banner.ImageDesktopUrl = dto.ImageDesktopBase64;
        if (dto.ImageMobileBase64 != null) banner.ImageMobileUrl = dto.ImageMobileBase64;

        await _daprClient.SaveStateAsync(StateStore, key, banners);

        return new BannerResponse
        {
            QlnBannersDailyHero = banners
        };
    }
    public async Task<bool> DeleteBannerFromStateAsync(string category, string code, CancellationToken ct = default)
    {
        var key = $"banner-{category.ToLower()}";

        var banners = await _daprClient.GetStateAsync<List<BannerItem>>(StateStore, key, cancellationToken: ct) ?? new();
        var bannerToDelete = banners.FirstOrDefault(b => b.Code == code);

        if (bannerToDelete == null)
            return false;

        banners.RemoveAll(b => b.Code == code);

        // Save updated list back
        await _daprClient.SaveStateAsync(StateStore, key, banners, cancellationToken: ct);
        return true;
    }


}
