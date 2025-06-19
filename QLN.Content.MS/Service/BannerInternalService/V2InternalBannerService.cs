using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;

namespace QLN.Content.MS.Service
{
    public class V2InternalBannerService : IV2contentBannerService
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<V2InternalBannerService> _logger;
        private const string StateStore = "contentstatestore";
        private const string IndexKey = "banner-index";

        public V2InternalBannerService(DaprClient daprClient, ILogger<V2InternalBannerService> logger)
        {
            _daprClient = daprClient;
            _logger = logger;
        }

        private static string GetCategoryKey(string category) => $"banner-{category.ToLower()}";

        public async Task<BannerResponse> SaveBannerAsync(BannerCreateRequest dto, string userId, CancellationToken ct = default)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Category) || string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(userId))
                    return null;

                var categoryKey = GetCategoryKey(dto.Category);

                var banners = await _daprClient.GetStateAsync<List<BannerItem>>(StateStore, categoryKey, cancellationToken: ct) ?? new();
                banners.RemoveAll(x => x.Code == dto.Code);

                banners.Add(new BannerItem
                {
                    Category = dto.Category,
                    Code = dto.Code,
                    Alt = dto.Alt,
                    Duration = dto.Duration,
                    ImageDesktopUrl = dto.ImageDesktopUrl,
                    ImageMobileUrl = dto.ImageMobileUrl,
                    Link = dto.Link,
                    CreatedBy = userId,
                    QueueName = dto.QueueName,
                    QueueLabel = dto.QueueLabel
                });

                await _daprClient.SaveStateAsync(StateStore, categoryKey, banners, cancellationToken: ct);

                var indexKeys = await _daprClient.GetStateAsync<List<string>>(StateStore, IndexKey, cancellationToken: ct) ?? new();
                if (!indexKeys.Contains(categoryKey))
                {
                    indexKeys.Add(categoryKey);
                    await _daprClient.SaveStateAsync(StateStore, IndexKey, indexKeys, cancellationToken: ct);
                }

                return new BannerResponse { QlnBannersDailyHero = banners };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving banner for category {Category}, code {Code}", dto?.Category, dto?.Code);
                return null;
            }
        }

        public async Task<List<BannerItem>> GetBannersByCategoryAsync(string category, CancellationToken ct = default)
        {
            try
            {
                var key = GetCategoryKey(category);
                return await _daprClient.GetStateAsync<List<BannerItem>>(StateStore, key, cancellationToken: ct) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching banners for category {Category}", category);
                return new();
            }
        }

        public async Task<BannerResponse> UpdateBannerAsync(BannerUpdateRequest dto, string userId, CancellationToken ct = default)
        {
            try
            {
                var key = GetCategoryKey(dto.Category);
                var banners = await _daprClient.GetStateAsync<List<BannerItem>>(StateStore, key, cancellationToken: ct) ?? new();

                var banner = banners.FirstOrDefault(x => x.Code == dto.Code);
                if (banner == null)
                {
                    _logger.LogWarning("Banner not found for update - category: {Category}, code: {Code}", dto.Category, dto.Code);
                    return null;
                }

                if (dto.Alt != null) banner.Alt = dto.Alt;
                if (dto.Category != null) banner.Category = dto.Category;
                if (dto.Code != null) banner.Code = dto.Code;
                if (dto.Duration != null) banner.Duration = dto.Duration;
                if (dto.Link != null) banner.Link = dto.Link;
                if (dto.ImageDesktopBase64 != null) banner.ImageDesktopUrl = dto.ImageDesktopBase64;
                if (dto.ImageMobileBase64 != null) banner.ImageMobileUrl = dto.ImageMobileBase64;

                await _daprClient.SaveStateAsync(StateStore, key, banners, cancellationToken: ct);

                return new BannerResponse { QlnBannersDailyHero = banners };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating banner {Category} - {Code}", dto?.Category, dto?.Code);
                return null;
            }
        }

        public async Task<bool> DeleteBannerFromStateAsync(string category, string code, CancellationToken ct = default)
        {
            try
            {
                var key = GetCategoryKey(category);
                var banners = await _daprClient.GetStateAsync<List<BannerItem>>(StateStore, key, cancellationToken: ct) ?? new();

                var bannerToDelete = banners.FirstOrDefault(b => b.Code == code);
                if (bannerToDelete == null)
                    return false;

                banners.RemoveAll(b => b.Code == code);
                await _daprClient.SaveStateAsync(StateStore, key, banners, cancellationToken: ct);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting banner {Category} - {Code}", category, code);
                return false;
            }
        }

        public async Task<Dictionary<string, BaseQueueResponse<BannerItem>>> GetAllBannersAsync(CancellationToken ct = default)
        {
            try
            {
                var index = await _daprClient.GetStateAsync<List<string>>(StateStore, IndexKey, cancellationToken: ct) ?? new();
                var allBanners = new List<BannerItem>();

                foreach (var key in index)
                {
                    var items = await _daprClient.GetStateAsync<List<BannerItem>>(StateStore, key, cancellationToken: ct) ?? new();
                    allBanners.AddRange(items);
                }

                return allBanners
                    .Where(x => !string.IsNullOrWhiteSpace(x.QueueName))
                    .GroupBy(x => x.QueueName)
                    .ToDictionary(
                        g => g.Key,
                        g => new BaseQueueResponse<BannerItem>
                        {
                            QueueLabel = g.FirstOrDefault()?.QueueLabel ?? g.Key,
                            Items = g.ToList()
                        });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all banners from state store");
                return new();
            }
        }
    }
}
