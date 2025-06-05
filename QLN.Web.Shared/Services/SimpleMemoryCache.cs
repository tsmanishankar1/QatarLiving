using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Services
{
    public class SimpleMemoryCache : ISimpleMemoryCache
    {
        private readonly IEventService _eventService;
        private IContentService _contentService;
        private readonly ILogger<SimpleMemoryCache> _logger;
        private readonly IMemoryCache _memoryCache;
        private const string BannerCacheKey = "BannerResponseCacheKey";
        private const string DailyCacheKey = "DailyNewsResponseCacheKey";
        private static readonly TimeSpan BannerCacheDuration = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan DailyCacheDuration = TimeSpan.FromMinutes(1);

        public SimpleMemoryCache(
            IEventService eventService,
            IContentService contentService,
            ILogger<SimpleMemoryCache> logger,
            IMemoryCache memoryCache)
        {
            _eventService = eventService;
            _contentService = contentService;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<ContentsDailyPageResponse?> GetContentLandingAsync()
        {
            if (_memoryCache.TryGetValue(DailyCacheKey, out ContentsDailyPageResponse? cachedDaily))
            {
                return cachedDaily;
            }

            var daily = await FetchContentData();
            if (daily != null)
            {
                _memoryCache.Set(DailyCacheKey, daily, DailyCacheDuration);
            }
            return daily;
        }

        public async Task<ContentsDailyPageResponse?> FetchContentData()
        {
            try
            {
                var result = await _contentService.GetDailyLPAsync();

                if (result != null && result.IsSuccessStatusCode && result.Content != null)
                {
                    return await result.Content.ReadFromJsonAsync<ContentsDailyPageResponse>();
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, "GetContentLandingAsync");
                return null;
            }
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
                _memoryCache.Set(BannerCacheKey, banner, BannerCacheDuration);
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
