using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

public class MoreArticleSectionBase : ComponentBase
{
    [Parameter]
    public List<ContentPost>? Articles { get; set; }

    [Parameter]
    public bool loading { get; set; }

    [Parameter]
    public string? category { get; set; }

    [Parameter]
    public string? subCategory { get; set; }

    [Parameter]
    public List<BannerItem>? ArticleSideBanners { get; set; } = new();

    [Parameter]
    public List<ContentEvent>? Events { get; set; } = new();

    [Inject]
    protected NavigationManager navManager { get; set; }

    protected bool imageLoaded = false;

    public ContentPost? selectedPost { get; set; }
    public ContentEvent? selectedEvent { get; set; }

    protected void SelectArticle(ContentPost article)
    {
        selectedPost = article;
    }

    protected override void OnParametersSet()
    {
        Console.WriteLine("the sub category is " + subCategory);
        Console.WriteLine("the category is " + category);
        imageLoaded = false;
        if (Events != null && Events.Any())
        {
            foreach (var ev in Events)
            {
                Console.WriteLine($"Event: {ev.Title}, Start: {ev.EventStart}, Venue: {ev.EventVenue}");
            }
        }
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
    protected void NavigateToEventArticle(ContentEvent article)
    {
        var url = $"/content/daily/article/details/{article.Slug}";
        navManager.NavigateTo(url, forceLoad: true);
    }
}