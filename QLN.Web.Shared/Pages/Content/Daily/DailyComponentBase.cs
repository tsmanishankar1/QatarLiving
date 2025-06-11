using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;


namespace QLN.Web.Shared.Pages.Content.Daily
{
    public class DailyComponentBase : ComponentBase
    {
        [Inject] private IContentService _contentService { get; set; }
        [Inject] private ISimpleMemoryCache _simpleCacheService { get; set; }

        protected ContentsDailyPageResponse LandingContent { get; set; } = new ContentsDailyPageResponse();
        protected ContentPost TopStoryItem { get; set; } = new ContentPost();
        protected ContentEvent HighlightedEvent { get; set; } = new ContentEvent();
        protected List<ContentEvent> FeaturedEvents { get; set; } = [];
        protected List<ContentEvent> MoreArticles { get; set; } = [];
        protected List<ContentVideo> VideoList { get; set; } = [];
        protected List<BannerItem> DailyHeroBanners { get; set; } = new();
        protected List<BannerItem> DailyTakeOver1Banners { get; set; } = new();
        protected List<BannerItem> DailyTakeOver2Banners { get; set; } = new();
        protected bool isLoadingBanners = true;

        protected List<string> carouselImages = new()
        {
        "/qln-images/banner_image.svg",
        "/qln-images/banner_image.svg",
        "/qln-images/banner_image.svg"
        };

        protected bool isLoading = true;

        protected List<ContentEvent> TopicQueue1 { get; set; } = [];
        protected List<ContentEvent> TopicQueue2 { get; set; } = [];
        protected List<ContentEvent> TopicQueue3 { get; set; } = [];
        protected List<ContentEvent> TopicQueue4 { get; set; } = [];
        protected List<ContentEvent> TopicQueue5 { get; set; } = [];

        protected string TopicQueue1Label { get; set; } = string.Empty;
        protected string TopicQueue2Label { get; set; } = string.Empty;
        protected string TopicQueue3Label { get; set; } = string.Empty;
        protected string TopicQueue4Label { get; set; } = string.Empty;
        protected string TopicQueue5Label { get; set; } = string.Empty;
        protected List<ContentPost> TopStories { get; set; } = [];
        protected List<ContentEvent> vMoreArticles { get; set; } = [];
        protected List<ContentVideo> vVideoList { get; set; } = [];
        protected string VideoQueueLabel { get; set; } = string.Empty;

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            isLoading = true;

            try
            {
                await Task.WhenAll(
                    LoadContent(),
                    LoadBanners()
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, "OnAfterRenderAsync");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task LoadContent()
        {
            LandingContent = await _simpleCacheService.GetContentLandingAsync() ?? new();
            TopStoryItem = LandingContent?.ContentsDaily?.DailyTopStory?.Items.First() ?? new();
            HighlightedEvent = LandingContent?.ContentsDaily?.DailyEvent?.Items.First() ?? new();
            FeaturedEvents = LandingContent?.ContentsDaily?.DailyFeaturedEvents?.Items ?? [];
            vMoreArticles = LandingContent?.ContentsDaily?.DailyMoreArticles?.Items ?? [];
            vVideoList = LandingContent?.ContentsDaily?.DailyWatchOnQatarLiving?.Items ?? [];
            TopicQueue1Label = LandingContent?.ContentsDaily?.DailyTopics1?.QueueLabel ?? "";
            TopicQueue2Label = LandingContent?.ContentsDaily?.DailyTopics2?.QueueLabel ?? "";
            TopicQueue3Label = LandingContent?.ContentsDaily?.DailyTopics3?.QueueLabel ?? "";
            TopicQueue4Label = LandingContent?.ContentsDaily?.DailyTopics4?.QueueLabel ?? "";
            TopicQueue1 = LandingContent?.ContentsDaily?.DailyTopics1?.Items ?? [];
            TopicQueue2 = LandingContent?.ContentsDaily?.DailyTopics2?.Items ?? [];
            TopicQueue3 = LandingContent?.ContentsDaily?.DailyTopics3?.Items ?? [];
            TopicQueue4 = LandingContent?.ContentsDaily?.DailyTopics4?.Items ?? [];
            var ListOfTopStories = LandingContent?.ContentsDaily?.DailyTopStories?.Items ?? [];

            TopStories = [.. ListOfTopStories.Take(3)]; // Just 3 should display

            TopicQueue5 = LandingContent?.ContentsDaily?.DailyTopics5?.Items ?? [];
            TopicQueue5Label = LandingContent?.ContentsDaily?.DailyTopics5?.QueueLabel ?? "";

            MoreArticles = [.. vMoreArticles.Take(4)];
            VideoQueueLabel = LandingContent?.ContentsDaily?.DailyWatchOnQatarLiving?.QueueLabel ?? string.Empty;
            VideoList = [.. vVideoList.Take(3)];
        }

        /// <summary>
        /// Gets Content Landing Page data
        /// </summary>
        /// <returns>QlnContentsDailyPageResponse</returns>
        
        //private async Task<BannerResponse?> FetchBannerData()
        //{
        //    try
        //    {
        //        var response = await _contentService.GetBannerAsync();
        //        if (response.IsSuccessStatusCode && response.Content != null)
        //        {
        //            return await response.Content.ReadFromJsonAsync<BannerResponse>();
        //        }
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"FetchBannerData error: {ex.Message}");
        //        return null;
        //    }
        //}

        protected async Task LoadBanners()
        {
            isLoadingBanners = true;
            try
            {
                var banners = await _simpleCacheService.GetBannerAsync();
                DailyHeroBanners = banners?.ContentDailyHero ?? new();
                DailyTakeOver1Banners = banners?.ContentDailyTakeoverFirst ?? new(); // these are currently empty from source
                DailyTakeOver2Banners = banners?.ContentDailyTakeoverSecond ?? new(); // these are currently empty from source
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading banners: {ex.Message}");
            }
            finally
            {
                isLoadingBanners = false;
            }
        }
    }
}
