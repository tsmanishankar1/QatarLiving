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
                    bool isReachable = await IsUrlReachableAsync(item.ImageUrl);
                    // Logger.LogInformation("Video URL: {Url} - Valid: {IsValid}", item.ImageUrl, isReachable);

                    VideoCards.Add(new VideoCard
                    {
                        VideoUrl = isReachable ? item.ImageUrl : null,
                        Title = item.Title,
                        TimeAgo = item.Description,
                        IsValid = isReachable
                    });
                }
            }
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
            if (video.IsPlaying)
            {
                await JSRuntime.InvokeVoidAsync("pauseVideo", video.VideoElement);
                video.IsPlaying = false;
                video.ShowPlayButton = true;
            }
            else
            {
                foreach (var v in VideoCards.Where(v => v != video && v.IsPlaying))
                {
                    await JSRuntime.InvokeVoidAsync("pauseVideo", v.VideoElement);
                    v.IsPlaying = false;
                    v.ShowPlayButton = true;
                }

                await JSRuntime.InvokeVoidAsync("playVideo", video.VideoElement);
                video.IsPlaying = true;
                video.ShowPlayButton = false;
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
            public ElementReference VideoElement { get; set; }
            public bool IsValid { get; set; } = false;
        }
    }
}
