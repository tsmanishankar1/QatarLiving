using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
public class MoreArticleBase : ComponentBase
{
    [Parameter]
    public List<ContentPost> Articles { get; set; } = new List<ContentPost>();
    [Inject]
    protected NavigationManager navManager { get; set; }

    [Parameter]
    public bool loading { get; set; } = false;

    [Parameter]
    public string selectedTab { get; set; }

    [Parameter] public EventCallback<ContentPost> OnClick { get; set; }
    protected void onclick(ContentPost news)
    {
        navManager.NavigateTo($"/content/article/details/{news.Slug}");
    }
}