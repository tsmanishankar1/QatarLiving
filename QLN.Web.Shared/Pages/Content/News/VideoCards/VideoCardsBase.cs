using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
public class VideoCardsBase : ComponentBase
{
    [Inject]
    protected IJSRuntime JSRuntime { get; set; }
    public class VideoItem
    {
        public string Title { get; set; }
        public string ThumbnailUrl { get; set; }
    }

    protected List<VideoItem> VideoList = new()
    {
        new() { Title = "How to spot scam websites & social media accounts in Qatar", ThumbnailUrl = "/images/sample_news.svg" },
        new() { Title = "Amir Cup Recap and Highlights", ThumbnailUrl = "/images/sample_news.svg" },
        new() { Title = "How to spot scam websites & social media accounts in Qatar", ThumbnailUrl = "/images/sample_news.svg" }
    };
    public VideoCard SingleVideoCard { get; set; } = new VideoCard
    {
    VideoUrl = "/videos/video1.mp4",
    Title = "Video 1"
    };

    protected VideoItem SelectedVideo;

    protected override void OnInitialized()
    {
        SelectedVideo = VideoList[1]; 
    }

    protected void SelectVideo(VideoItem video)
    {
        SelectedVideo = video;
    }
    public class VideoCard
    {
        public string VideoUrl { get; set; }
        public string Title { get; set; }
        public bool ShowPlayButton { get; set; }
        public bool IsPlaying { get; set; }
        public ElementReference VideoElement { get; set; }
    }
    protected async Task ToggleVideoPlay(VideoCard video)
    {
        if (video.IsPlaying)
        {
            await JSRuntime.InvokeVoidAsync("pauseVideo", video.VideoElement);
            video.IsPlaying = false;
            video.ShowPlayButton = true; // Show play button again when paused
        }
        else
        {
            
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

}