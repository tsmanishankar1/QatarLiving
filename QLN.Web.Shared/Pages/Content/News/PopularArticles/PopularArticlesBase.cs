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
    protected void onclick(ContentPost news)
    {
        navManager.NavigateTo($"/article/details/{news.Slug}");
    }
}