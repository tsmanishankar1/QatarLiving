using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Helpers;
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
        private INewsService _newsService;
        private readonly ILogger<SimpleMemoryCache> _logger;
        private readonly IMemoryCache _memoryCache;
        private const string BannerCacheKey = "BannerResponseCacheKey";
        private const string DailyCacheKey = "DailyNewsResponseCacheKey";
        private const string NewsCachePrefix = "News";
        private static readonly TimeSpan BannerCacheDuration = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan DailyCacheDuration = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan NewsCacheDuration = TimeSpan.FromMinutes(2);
        private readonly NavigationPath _navigationPath;

        public SimpleMemoryCache(
            IEventService eventService,
            IContentService contentService,
            INewsService newsService,
            ILogger<SimpleMemoryCache> logger,
            IMemoryCache memoryCache,
            IOptions<NavigationPath> navigationPath
            )
        {
            _eventService = eventService;
            _contentService = contentService;
            _newsService = newsService;
            _logger = logger;
            _memoryCache = memoryCache;
            _navigationPath = navigationPath.Value;
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
                HttpResponseMessage? result;
                if (_navigationPath.ContentDaily.Contains("v2", StringComparison.OrdinalIgnoreCase))
                {
                    result = await _contentService.GetDailyLPV2Async();
                }
                else
                {
                    result = await _contentService.GetDailyLPAsync();
                }

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

        public async Task<GenericNewsPageResponse?> GetCurrentNews(string tab)
        {
            var suffix = TextHelper.ToSnakeCase(tab);
            if (_memoryCache.TryGetValue($"{NewsCachePrefix.ToLower()}_{suffix}", out GenericNewsPageResponse? newsTab))
            {
                return newsTab;
            }

            var newsTabPage = await FetchNewsData(tab);
            
            if (newsTabPage != null)
            {
                _memoryCache.Set($"{NewsCachePrefix.ToLower()}_{suffix}", newsTabPage, NewsCacheDuration);
            }

            return newsTabPage;
        }

        private async Task<GenericNewsPageResponse?> FetchNewsData(string tab)
        {
            try
            {
                var apiResponse = await _newsService.GetNewsAsync(tab) ?? new HttpResponseMessage();
                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<GenericNewsPageResponse>();
                    return response ?? new ();
                }
                return new ();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"FetchNewsData failed for tab: {tab}");
                return new ();
            }
        }
    }
}
