using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
public class MoreArticleBase : ComponentBase
{
    [Parameter]
    public List<ContentPost> Articles { get; set; } = new List<ContentPost>();
    [Inject]
    protected NavigationManager navManager { get; set; }
    protected bool imageLoaded = false;
    protected bool imageFailed = false;
    protected string? currentImageUrl;

    [Parameter]
    public bool loading { get; set; } = false;

    [Parameter]
    public string selectedTab { get; set; }
    [Parameter]
    public string selectedMainTab { get; set; }

    [Parameter] public EventCallback<ContentPost> OnClick { get; set; }
    protected void onclick(ContentPost news)
    {
        navManager.NavigateTo($"/content/article/details/{news.Slug}?category={selectedMainTab}&subcategory={selectedTab}");
    }
    protected override void OnParametersSet()
    {
        imageLoaded = true;
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
    // protected bool ShowEmptyCard =>
    //     string.IsNullOrWhiteSpace(Item?.ImageUrl) || imageFailed;
}