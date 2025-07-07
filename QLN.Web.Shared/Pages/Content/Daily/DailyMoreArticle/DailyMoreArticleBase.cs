using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
public class DailyMoreArticleBase : ComponentBase
{
    [Inject] NavigationManager NavigationManager { get; set; }
    [Parameter]
    public List<ContentEvent> Items { get; set; } = [];
    [Parameter]
    public bool isLoading { get; set; } = false;

    protected void NavigatetoArticle()
    {
        NavigationManager.NavigateTo("content/events");
    }

    protected void NavigatetoArticle(ContentEvent article)
    {
        NavigationManager.NavigateTo($"/content/daily/article/details/{article.Slug}");
    }

}