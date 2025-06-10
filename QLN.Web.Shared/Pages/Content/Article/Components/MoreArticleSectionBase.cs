using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
public class MoreArticleSectionBase : ComponentBase
{
    [Parameter]
    public List<ContentPost> Articles { get; set; } = new List<ContentPost>();
    [Parameter]
    public bool loading { get; set; }
    [Parameter]
    public string category { get; set; }
    [Inject]
    protected NavigationManager navManager { get; set; }
    [Parameter]
    public string subCategory { get; set; }
    protected bool imageLoaded = false;
    public ContentPost selectedPost { get; set; }
    [Parameter]
    public List<BannerItem> ArticleSideBanners { get; set; } = new();
    protected void SelectArticle(ContentPost article)
    {
        selectedPost = article;
    }
    protected override void OnParametersSet()
    {
        Console.WriteLine("the sub category is " + subCategory);
        Console.WriteLine("the category is " + category);
        imageLoaded = false;
    }
    protected void OnImageLoaded()
    {
        imageLoaded = true;
        StateHasChanged();
    }

    protected void OnImageError()
    {
        imageLoaded = true;
        StateHasChanged();
    }
    protected void NavigateToArticle(ContentPost article)
    {
        var url = $"/content/article/details/{article.Slug}?category={category}&subcategory={subCategory}";
        navManager.NavigateTo(url, forceLoad: true);
    }

}