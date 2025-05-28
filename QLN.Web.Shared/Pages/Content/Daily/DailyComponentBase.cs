using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Pages.Content.Daily
{
    public class DailyComponentBase : LayoutComponentBase
    {
        [Inject] private IContentService _contentService { get; set; }

        protected QlnContentsDailyPageResponse LandingContent { get; set; } = new QlnContentsDailyPageResponse();
        protected ContentPost TopStoryItem { get; set; } = new ContentPost();
        protected ContentEvent HighlightedEvent { get; set; } = new ContentEvent();
        protected List<ContentPost> FeaturedEvents { get; set; } = [];
        protected List<ContentPost> MoreArticles { get; set; } = [];
        protected List<ContentPost> Videos { get; set; } = [];

        protected async override Task OnInitializedAsync()
        {
            try
            {
                LandingContent = await GetContentLandingAsync() ?? new();
                TopStoryItem = LandingContent.QlnContentsDaily?.DailyTopStory?.Items.First() ?? new();
                HighlightedEvent = LandingContent.QlnContentsDaily?.DailyEvent?.Items.First() ?? new();
                FeaturedEvents = LandingContent.QlnContentsDaily?.DailyFeaturedEvents?.Items ?? [];
                MoreArticles = LandingContent.QlnContentsDaily?.DailyMoreArticles?.Items ?? [];
                Videos = LandingContent.QlnContentsDaily?.DailyWatchOnQatarLiving.Items ?? [];
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
        protected async Task<QlnContentsDailyPageResponse> GetContentLandingAsync()
        {
            try
            {
                var apiResponse = await _contentService.GetDailyLPAsync() ?? new HttpResponseMessage();

                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<QlnContentsDailyPageResponse>();
                    return response ?? new QlnContentsDailyPageResponse();
                }

                return new QlnContentsDailyPageResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, "OnInitializedAsync");
                return new QlnContentsDailyPageResponse();
            }
        }
    }
}
