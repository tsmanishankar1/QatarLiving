using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services;

public class DailyVideoCardsBase : ComponentBase
{
    [Inject] NavigationManager NavigationManager { get; set; }

    [Inject] IOptions<NavigationPath> Options { get; set; }
    [Parameter] public List<ContentVideo> Items { get; set; }

    protected ContentVideo SelectedVideo;
    protected NavigationPath NavigationPath => Options.Value;

    protected override void OnInitialized()
    {
        SelectedVideo = Items.FirstOrDefault();
    }

    protected void SelectVideo(ContentVideo video)
    {
        SelectedVideo = video;
    }

    protected void NavigatetoArticle(ContentVideo video)
    {
        NavigationManager.NavigateTo($"/article/details/{video.Slug}");
    }

    protected void OnClickViewAll()
    {
        NavigationManager.NavigateTo(NavigationPath.AllVideos);
    }
}