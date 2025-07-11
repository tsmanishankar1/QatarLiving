using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services;
using System.Runtime.CompilerServices;
using System.Web;

namespace QLN.Web.Shared.Pages.Content.DailyV2.DailyVideoCards
{
    public class DailyVideoCardsBase : ComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject] IOptions<NavigationPath> Options { get; set; }
        [Inject] ILogger<DailyVideoCardsBase> Logger { get; set; }
        [Parameter] public List<ContentVideo> Items { get; set; }

        [Parameter] public ContentVideo SelectedVideo { get; set; }
        protected NavigationPath NavigationPath => Options.Value;

        protected bool IsVisiblePlayButton { get; set; } = true;

        [Parameter] public string QueueLabel { get; set; }

        protected string YTVideoEmbedURL { get; set; } = string.Empty;

        protected override void OnInitialized()
        {
            SelectedVideo = Items?.FirstOrDefault() ?? new();

            YTVideoEmbedURL = ConvertToEmbedUrl(SelectedVideo.VideoUrl);
        }

        protected async override Task OnParametersSetAsync()
        {

            try
            {
                SelectedVideo = new();
                YTVideoEmbedURL = string.Empty;
                SelectedVideo = Items?.FirstOrDefault() ?? new();
                YTVideoEmbedURL = ConvertToEmbedUrl(SelectedVideo.VideoUrl);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnParametersSetAsync");
            }
        }

        protected void PlayVideo(ContentVideo video)
        {
            SelectedVideo = video;
            YTVideoEmbedURL = string.Empty;
            IsVisiblePlayButton = false;

            YTVideoEmbedURL = ConvertToEmbedUrl(SelectedVideo.VideoUrl);
        }

        /// <summary>
        /// Converts Youtube Shorts or Video as a Embed URL.
        /// </summary>
        /// <param name="url">youtube url</param>
        /// <returns>Youtube Embed URL</returns>
        protected static string ConvertToEmbedUrl(string url)
        {
            try
            {
                string videoID = string.Empty;
                string youtubeEmbedPreset = "https://www.youtube.com/embed/";

                if (!string.IsNullOrEmpty(url) && url.Contains("https://www.youtube.com/"))
                {
                    if (url.Contains("shorts"))
                    {
                        // Get VideoID
                        videoID = url.Replace("https://www.youtube.com/shorts/", string.Empty).Trim();
                    }
                    else
                    {
                        // Get VideoID if it is a video ID
                        videoID = url.Replace("https://www.youtube.com/watch?v=", string.Empty).Trim();
                    }

                    return $"{youtubeEmbedPreset}{videoID}";
                }

                return url;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message} ConvertToEmbedUrl");
                return url;
            }
        }

        protected void OnClickViewAll()
        {
            NavigationManager.NavigateTo(NavigationPath.AllVideos);
        }
    }
}