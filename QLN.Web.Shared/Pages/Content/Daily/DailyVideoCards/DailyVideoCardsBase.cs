using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services;

public class DailyVideoCardsBase : ComponentBase
{
    [Inject] NavigationManager NavigationManager { get; set; }

    [Inject] IOptions<NavigationPath> Options { get; set; }
    [Parameter] public List<ContentPost> Items { get; set; }

    protected ContentPost SelectedVideo;
    protected NavigationPath NavigationPath => Options.Value;

    protected override void OnInitialized()
    {
        SelectedVideo = Items.FirstOrDefault();
    }

    protected void SelectVideo(ContentPost video)
    {
        SelectedVideo = video;
    }

    protected void NavigatetoArticle(ContentPost video)
    {
        NavigationManager.NavigateTo($"/article/details/{video.Slug}");
    }

    protected void OnClickViewAll()
    {
        NavigationManager.NavigateTo(NavigationPath.AllVideos);
    }
}