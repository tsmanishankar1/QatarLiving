using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
public class DailyMoreArticleBase : ComponentBase
{
    [Inject] NavigationManager NavigationManager { get; set; }
    [Parameter]
    public List<ContentPost> Items { get; set; } = [];

    protected void NavigatetoArticle()
    {
        NavigationManager.NavigateTo("content/events");
    }

    protected void NavigatetoArticle(ContentPost article)
    {
        NavigationManager.NavigateTo($"/article/details/{article.Slug}");
    }

}