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

        protected List<ContentEvent> TopicQueue1 { get; set; } = [];
        protected List<ContentEvent> TopicQueue2 { get; set; } = [];
        protected List<ContentEvent> TopicQueue3 { get; set; } = [];
        protected List<ContentEvent> TopicQueue4 { get; set; } = [];

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
