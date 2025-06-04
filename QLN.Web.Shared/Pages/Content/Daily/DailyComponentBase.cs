using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;


namespace QLN.Web.Shared.Pages.Content.Daily
{
    public class DailyComponentBase : ComponentBase
    {
        [Inject] private IContentService _contentService { get; set; }

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

        protected async override Task OnInitializedAsync()
        {
            isLoading = true;
            try
            {
                LandingContent = await GetContentLandingAsync() ?? new();
                TopStoryItem = LandingContent?.ContentsDaily?.DailyTopStory?.Items.First() ?? new();
                HighlightedEvent = LandingContent?.ContentsDaily?.DailyEvent?.Items.First() ?? new();
                FeaturedEvents = LandingContent?.ContentsDaily?.DailyFeaturedEvents?.Items ?? [];
                MoreArticles = LandingContent?.ContentsDaily?.DailyMoreArticles?.Items ?? [];
                VideoList = LandingContent?.ContentsDaily?.DailyWatchOnQatarLiving?.Items ?? [];
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
               
                await LoadBanners();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, "OnInitializedAsync");
            }
            finally
            {
                isLoading = false;
            }
        }

        /// <summary>
        /// Gets Content Landing Page data
        /// </summary>
        /// <returns>QlnContentsDailyPageResponse</returns>
        protected async Task<ContentsDailyPageResponse> GetContentLandingAsync()
        {
            try
            {
                var apiResponse = await _contentService.GetDailyLPAsync() ?? new HttpResponseMessage();

                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<ContentsDailyPageResponse>();
                    return response ?? new ContentsDailyPageResponse();
                }

                return new ContentsDailyPageResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, "GetContentLandingAsync");
                return new ContentsDailyPageResponse();
            }
        }
        private async Task<BannerResponse?> FetchBannerData()
        {
            try
            {
                var response = await _contentService.GetBannerAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return await response.Content.ReadFromJsonAsync<BannerResponse>();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FetchBannerData error: {ex.Message}");
                return null;
            }
        }

        protected async Task LoadBanners()
        {
            isLoadingBanners = true;
            try
            {
                var banners = await FetchBannerData();
                DailyHeroBanners = banners?.ContentDailyHero ?? new();
                DailyTakeOver1Banners = banners?.ContentDailyTakeoverFirst ?? new();
                DailyTakeOver2Banners = banners?.ContentDailyTakeoverSecond ?? new();
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
