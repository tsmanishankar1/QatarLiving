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
    protected List<VideoCard> VideoList = new()
{
    new VideoCard 
    { 
        Title = "How to spot scam websites & social media accounts in Qatar", 
        VideoUrl = "/videos/video1.mp4", 
        ThumbnailUrl = "/images/sample_news.svg",
        ShowPlayButton = true,
        IsPlaying = false,
        VideoElement = default 
    },
    new VideoCard 
    { 
        Title = "Amir Cup Recap and Highlights", 
        VideoUrl = "/videos/video2.mp4", 
        ThumbnailUrl = "/images/sample_news.svg",
        ShowPlayButton = true,
        IsPlaying = false,
        VideoElement = default
    },
    new VideoCard 
    { 
        Title = "How to spot scam websites & social media accounts in Qatar", 
        VideoUrl = "/videos/video3.mp4", 
        ThumbnailUrl = "/images/sample_news.svg",
        ShowPlayButton = true,
        IsPlaying = false,
        VideoElement = default
    }
};
    public VideoCard SingleVideoCard { get; set; } = new VideoCard
    {
        VideoUrl = "/videos/video1.mp4",
        Title = "Video 1"
    };

    protected VideoCard SelectedVideo;

    protected override void OnInitialized()
    {
        SelectedVideo = VideoList[1];
    }

    protected void SelectVideo(VideoCard video)
    {
        SelectedVideo = video;
    }
    public class VideoCard
    {
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
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