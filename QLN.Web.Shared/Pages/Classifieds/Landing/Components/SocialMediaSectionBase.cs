using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Common.DTO_s;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace QLN.Web.Shared.Pages.Classifieds.Landing.Components
{
    public class SocialMediaSectionBase : ComponentBase
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public HttpClient Http { get; set; }
        [Inject] public ILogger<SocialMediaSectionBase> Logger { get; set; }

        [Parameter] public IEnumerable<LandingBackOfficeIndex>? SocialPostDetails { get; set; }
        [Parameter] public IEnumerable<LandingBackOfficeIndex>? SocialLinksList { get; set; }
        [Parameter] public IEnumerable<LandingBackOfficeIndex>? SocialMediaVideosList { get; set; }
        [Parameter] public bool Loading { get; set; }
        
        public string Title { get; set; } = "Are you Following Classifieds on Social Media?";
        public string Description { get; set; } = "Stay Tuned with Qatar Living Classifieds on social media for the latest updates, featured exclusive listings, market trends, and valuable insights to guide your investments.";
        public string AvatarUrl { get; set; } = "/qln-images/qatar_socialmedia_logo.svg";
        public string SourceName { get; set; } = "Qatar Living® Services";

        public List<LandingBackOfficeIndex> SocialLinks { get; set; } = new();
        public List<VideoCard> VideoCards { get; set; } = new();

        protected override async Task OnParametersSetAsync()
        {
            if (SocialPostDetails?.Any() == true)
            {
                Title = SocialPostDetails.First().Title ?? Title;
                Description = SocialPostDetails.First().Description ?? Description;
            }

            if (SocialLinksList?.Any() == true)
            {
                SocialLinks = SocialLinksList.ToList();
            }

            if (SocialMediaVideosList?.Any() == true)
            {
                var selectedVideos = SocialMediaVideosList.Take(3).ToList();

                VideoCards.Clear(); // Clear before re-populating to prevent duplication

                foreach (var item in selectedVideos)
                {
                     string? embedUrl = GetEmbeddableYouTubeUrl(item.ImageUrl, out var videoId);
                    bool isReachable = await IsUrlReachableAsync(embedUrl);
                    // Logger.LogInformation("Video URL: {Url} - Valid: {IsValid}", item.ImageUrl, isReachable);
 var playerId = $"player-{Guid.NewGuid()}";
                    VideoCards.Add(new VideoCard
                    {
                        VideoUrl = isReachable ? item.ImageUrl : null,
                        Title = item.Title,
                        TimeAgo = item.Description,
                        IsValid = isReachable,
                         PlayerId = playerId,
                        VideoId = videoId
                    });
                }
            }
        }
private string? GetEmbeddableYouTubeUrl(string? url, out string videoId)
{
    videoId = string.Empty;

    if (string.IsNullOrWhiteSpace(url))
        return null;

    try
    {
        var uri = new Uri(url);

        if (uri.Host.Contains("youtube.com") || uri.Host.Contains("youtu.be"))
        {
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

            // Watch format
            if (query.TryGetValue("v", out var v))
            {
                videoId = v.ToString();
            }
            // Shorts format
            else if (uri.Segments.Length > 1 && uri.Segments[1].Trim('/').Equals("shorts", StringComparison.OrdinalIgnoreCase))
            {
                videoId = uri.Segments.Last().Trim('/');
            }
            // youtu.be format
            else if (uri.Host.Contains("youtu.be") && uri.Segments.Length >= 2)
            {
                videoId = uri.Segments[1].Trim('/');
            }

            if (!string.IsNullOrEmpty(videoId))
            {
                return $"https://www.youtube.com/embed/{videoId}?enablejsapi=1";
            }
        }
    }
    catch
    {
        // fallback in case of error
    }

    return null;
}



        private async Task<bool> IsUrlReachableAsync(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                request.Headers.Add("User-Agent", "QLN-BlazorApp");

                var response = await Http.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to check URL: {Url}", url);
                return false;
            }
        }

       protected async Task ToggleVideoPlay(VideoCard video)
{
    foreach (var v in VideoCards.Where(v => v != video && v.IsPlaying))
    {
        await JSRuntime.InvokeVoidAsync("pauseVideo", v.PlayerId);
        v.IsPlaying = false;
        v.ShowPlayButton = true;
    }

    if (!video.IsPlaying)
    {
        await JSRuntime.InvokeVoidAsync("playVideo", video.PlayerId);
        video.IsPlaying = true;
        video.ShowPlayButton = false;
    }
    else
    {
        await JSRuntime.InvokeVoidAsync("pauseVideo", video.PlayerId);
        video.IsPlaying = false;
        video.ShowPlayButton = true;
    }

    StateHasChanged();
}


        protected void ShowPlayButton(VideoCard video)
        {
            if (!video.IsPlaying)
            {
                video.ShowPlayButton = true;
                StateHasChanged();
            }
        }

        protected void HidePlayButton(VideoCard video)
        {
            if (!video.IsPlaying)
            {
                video.ShowPlayButton = false;
                StateHasChanged();
            }
        }

        public class VideoCard
        {
            public string? VideoUrl { get; set; }
            public string? Title { get; set; }
            public string? TimeAgo { get; set; }
            public bool ShowPlayButton { get; set; }
            public bool IsPlaying { get; set; }
               public string PlayerId { get; set; } = string.Empty; // For JS interop
    public string VideoId { get; set; } = string.Empty;  // For data attr
            public ElementReference VideoElement { get; set; }
            public bool IsValid { get; set; } = false;
        }
    }
}
