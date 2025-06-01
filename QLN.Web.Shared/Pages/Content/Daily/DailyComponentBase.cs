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
        protected List<ContentPost> FeaturedEvents { get; set; } = [];
        protected List<ContentPost> MoreArticles { get; set; } = [];
        protected List<ContentPost> Videos { get; set; } = [];
        protected List<string> carouselImages = new()
        {
        "/qln-images/banner_image.svg",
        "/qln-images/banner_image.svg",
        "/qln-images/banner_image.svg"
        };

        protected bool isLoading = false;

        protected List<ContentPost> TopicQueue1 { get; set; } = [];
        protected List<ContentPost> TopicQueue2 { get; set; } = [];
        protected List<ContentPost> TopicQueue3 { get; set; } = [];
        protected List<ContentPost> TopicQueue4 { get; set; } = [];

        protected string TopicQueue1Label { get; set; } = string.Empty;
        protected string TopicQueue2Label { get; set; } = string.Empty;
        protected string TopicQueue3Label { get; set; } = string.Empty;
        protected string TopicQueue4Label { get; set; } = string.Empty;

        protected async override Task OnInitializedAsync()
        {
            try
            {
                LandingContent = await GetContentLandingAsync() ?? new();
                TopStoryItem = LandingContent?.ContentsDaily?.DailyTopStory?.Items.First() ?? new();
                HighlightedEvent = LandingContent?.ContentsDaily?.DailyEvent?.Items.First() ?? new();
                FeaturedEvents = LandingContent?.ContentsDaily?.DailyFeaturedEvents?.Items ?? [];
                MoreArticles = LandingContent?.ContentsDaily?.DailyMoreArticles?.Items ?? [];
                Videos = LandingContent?.ContentsDaily?.DailyWatchOnQatarLiving?.Items ?? [];
                TopicQueue1Label = LandingContent?.ContentsDaily?.DailyTopics1?.QueueLabel ?? "";
                TopicQueue2Label = LandingContent?.ContentsDaily?.DailyTopics2?.QueueLabel ?? "";
                TopicQueue3Label = LandingContent?.ContentsDaily?.DailyTopics3?.QueueLabel ?? "";
                TopicQueue4Label = LandingContent?.ContentsDaily?.DailyTopics4?.QueueLabel ?? "";
                TopicQueue1 = LandingContent?.ContentsDaily?.DailyTopics1?.Items ?? [];
                TopicQueue2 = LandingContent?.ContentsDaily?.DailyTopics2?.Items ?? [];
                TopicQueue3 = LandingContent?.ContentsDaily?.DailyTopics3?.Items ?? [];
                TopicQueue4 = LandingContent?.ContentsDaily?.DailyTopics4?.Items ?? [];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, "OnInitializedAsync");
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
                Console.WriteLine(ex.Message, "OnInitializedAsync");
                return new ContentsDailyPageResponse();
            }
        }

    }
}
