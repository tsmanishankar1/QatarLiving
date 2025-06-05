using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services;

public class DailyVideoCardsBase : ComponentBase
{
    [Inject] NavigationManager NavigationManager { get; set; }

    [Inject] IOptions<NavigationPath> Options { get; set; }
    [Inject] ILogger<DailyVideoCardsBase> Logger { get; set; }
    [Parameter] public List<ContentVideo> Items { get; set; }

    protected ContentVideo SelectedVideo;
    protected NavigationPath NavigationPath => Options.Value;

    protected bool IsVisiblePlayButton { get; set; } = true;

    [Parameter] public string QueueLabel { get; set; }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        try
        {
            SelectedVideo = Items?.Where(x => !string.IsNullOrEmpty(x.VideoUrl)).FirstOrDefault() ?? new();

            SelectedVideo.VideoUrl = ConvertToEmbedUrl(SelectedVideo.VideoUrl);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "OnAfterRenderAsync");
        }
    }

    protected void PlayVideo(ContentVideo video)
    {
        IsVisiblePlayButton = false;
        SelectedVideo = video;

        SelectedVideo.VideoUrl = ConvertToEmbedUrl(video.VideoUrl);
    }

    protected string ConvertToEmbedUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            string videoId = query["v"];

            if (!string.IsNullOrEmpty(videoId))
            {
                return $"https://www.youtube.com/embed/{videoId}";
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