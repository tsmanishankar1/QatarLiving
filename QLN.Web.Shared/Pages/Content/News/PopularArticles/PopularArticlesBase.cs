using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
public class PopularArticlesBase : ComponentBase
{
    [Parameter]
    public List<ContentPost> Articles { get; set; } = new List<ContentPost>();
    [Inject]
    protected NavigationManager navManager { get; set; }
    [Parameter]
    public string selectedTab { get; set; }
    [Parameter]
    public string selectedMainTab { get; set; }
    [Parameter]
    public bool loading { get; set; } = false;
    protected bool imageLoaded = false;
    protected bool imageFailed = false;


    protected void onclick(ContentPost news)
    {
        navManager.NavigateTo($"/content/article/details/{news.Slug}?category={selectedMainTab}&subcategory={selectedTab}");
    }
    protected override void OnParametersSet()
    {
        imageLoaded = false;
        imageFailed = false;
    }

    protected void OnImageLoaded()
    {
        imageLoaded = true;
        imageFailed = false;
        StateHasChanged();
    }

    protected void OnImageError()
    {
        imageLoaded = true;
        imageFailed = true;
        StateHasChanged();
    }
}