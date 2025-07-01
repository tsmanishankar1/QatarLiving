using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;

public class CategoryAdSectionBase : ComponentBase
{
    [Inject]
    private ILogger<CategoryAdSectionBase> Logger { get; set; } = default!;

    [Parameter]
    public List<LandingBackOfficeIndex> Categories { get; set; } = new();

    [Parameter]
    public EventCallback<LandingBackOfficeIndex> OnCategoryClick { get; set; }
    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    public bool Loading { get; set; }

   protected void HandleClick(LandingBackOfficeIndex item)
    {
        if (item?.EntityId == null)
            return;

        var url = $"/qln/classifieds/items?categoryId={item.EntityId}";
        NavigationManager.NavigateTo(url);
    }


  protected override void OnParametersSet()
{
    base.OnParametersSet();

    if (Categories == null || !Categories.Any())
    {
        Logger.LogWarning("CategoryAdSectionBase: Categories list is empty or null.");
    }
    else
    {
        Logger.LogInformation("CategoryAdSectionBase: Loaded {Count} categories.", Categories.Count);

        // Optional: Log first item info
        var first = Categories.FirstOrDefault();
        if (first != null)
        {
            Logger.LogDebug("First category - Title: {Title}, ImageUrl: {ImageUrl}", first.Title, first.ImageUrl);
        }
    }
}

}
