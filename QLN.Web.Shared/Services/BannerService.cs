using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Services
{
    public class BannerService : IBannerService
    {
        private readonly IEventService _eventService;
        private readonly ILogger<BannerService> _logger;
        private readonly IMemoryCache _memoryCache;
        private const string BannerCacheKey = "BannerResponseCacheKey";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public BannerService(
            IEventService eventService,
            ILogger<BannerService> logger,
            IMemoryCache memoryCache)
        {
            _eventService = eventService;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<BannerResponse?> GetBannerAsync()
        {
            if (_memoryCache.TryGetValue(BannerCacheKey, out BannerResponse? cachedBanner))
            {
                return cachedBanner;
            }

            var banner = await FetchBannerData();
            if (banner != null)
            {
                _memoryCache.Set(BannerCacheKey, banner, CacheDuration);
            }
            return banner;
        }

        private async Task<BannerResponse?> FetchBannerData()
        {
            try
            {
                var result = await _eventService.GetBannerAsync();
                if (result != null && result.IsSuccessStatusCode && result.Content != null)
                {
                    return await result.Content.ReadFromJsonAsync<BannerResponse>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FetchBannerData error.");
                return null;
            }
        }
    }
}
