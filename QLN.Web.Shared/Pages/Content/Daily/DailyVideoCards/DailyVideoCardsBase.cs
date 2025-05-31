using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Common.Infrastructure.DTO_s;

public class DailyVideoCardsBase : ComponentBase
{
    [Inject]
    protected IJSRuntime JSRuntime { get; set; }

    [Parameter] public List<ContentPost> Items { get;set; }
    public class VideoItem
    {
        public string Title { get; set; }
        public string ThumbnailUrl { get; set; }
    }

    protected ContentPost SelectedVideo;

    protected override void OnInitialized()
    {
        SelectedVideo = Items.FirstOrDefault();
    }

    protected void SelectVideo(ContentPost video)
    {
        SelectedVideo = video;
    }

    protected async Task ToggleVideoPlay(ContentPost video)
    {
       
    }

    protected void ShowPlayButton(ContentPost video)
    {

    }

    protected void HidePlayButton(ContentPost video)
    {

    }

}