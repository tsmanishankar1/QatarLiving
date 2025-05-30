using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using Microsoft.JSInterop;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
public class VideoCardsBase : ComponentBase
{
    [Parameter]
    public List<ContentPost> Articles { get; set; } = new List<ContentPost>();
    [Parameter] public EventCallback<ContentPost> OnClick { get; set; }
    [Parameter]
    public string selectedTab { get; set; }
    protected NavigationManager navManager { get; set; }
    protected ContentPost SelectedArticle { get; set; }
    

    protected override void OnParametersSet()
    {
        if (Articles != null && Articles.Any())
        {
            SelectedArticle = Articles.First();
        }
    }

    protected void SelectVideo(ContentPost article)
    {
        SelectedArticle = article;
    }
    protected void onclick(ContentPost news)
    {
        navManager.NavigateTo($"/article/details/{Uri.EscapeDataString(news.Slug)}/{selectedTab}");
    }
}