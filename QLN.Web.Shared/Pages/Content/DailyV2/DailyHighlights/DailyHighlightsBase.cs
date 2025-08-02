using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Components;

namespace QLN.Web.Shared.Pages.Content.DailyV2.DailyHighlights
{
    public class DailyHighlightsBase : QLComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Parameter]
        public string QueueLabel {  get; set; }

        [Parameter]
        public List<ContentEvent> ListOfItems { get; set; }

        protected void NavigateToDetailPage(ContentEvent item)
        {
            if (item.NodeType.Contains("post") && !string.IsNullOrWhiteSpace(item.Slug))
            {
                NavigationManager.NavigateTo($"{NavigationPath.Value.ContentNewsDailyDetails}{item.Slug}");
            }
            else if (item.NodeType.Contains("event") && !string.IsNullOrWhiteSpace(item.Slug))
            {
                NavigationManager.NavigateTo($"{NavigationPath.Value.ContentEventsDetail}{item.Slug}");
            }
        }

        /// <summary>
        /// Converts various YouTube URLs (Shorts, watch links, youtu.be) into an embed URL.
        /// </summary>
        /// <param name="url">YouTube video URL</param>
        /// <returns>Embed URL</returns>
        protected static string ConvertToEmbedUrl(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                    return url;

                string videoID = string.Empty;
                string youtubeEmbedPreset = "https://www.youtube.com/embed/";

                // Handle YouTube Shorts
                if (url.Contains("youtube.com/shorts/"))
                {
                    videoID = ExtractVideoId(url, "youtube.com/shorts/");
                }
                // Handle YouTube Watch links
                else if (url.Contains("youtube.com/watch?v="))
                {
                    videoID = ExtractVideoId(url, "youtube.com/watch?v=");
                }
                // Handle youtu.be short links
                else if (url.Contains("youtu.be/"))
                {
                    videoID = ExtractVideoId(url, "youtu.be/");
                }

                return string.IsNullOrWhiteSpace(videoID) ? url : $"{youtubeEmbedPreset}{videoID}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message} ConvertToEmbedUrl");
                return url;
            }
        }

        /// <summary>
        /// Helper method to extract video ID from a given URL part and remove query string.
        /// </summary>
        private static string ExtractVideoId(string url, string marker)
        {
            var startIndex = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0) return string.Empty;

            var idPart = url.Substring(startIndex + marker.Length);
            var videoId = idPart.Split('?', '&')[0].Trim();

            return videoId;
        }
    }
}
