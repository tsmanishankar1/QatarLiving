using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Web.Shared.Components.Classifieds.FeaturedItemCard;
using QLN.Web.Shared.Services;
using static QLN.Web.Shared.Helpers.HttpErrorHelper;

public class SearchBarSectionBase : ComponentBase
{
    [Inject] protected ISnackbar Snackbar { get; set; }

    [Parameter] public EventCallback<List<FeaturedItemCard.FeaturedItem>> OnSearchCompleted { get; set; }

    protected string searchText;
    protected string selectedCategory;
    protected bool loading = false;

 protected List<CategoryItem> categoryOptions = new()
{
    new CategoryItem { Id = "mobiles", Label = "Mobile Phones & Tablets" },
    new CategoryItem { Id = "accessories", Label = "Accessories" },
    new CategoryItem { Id = "fashion", Label = "Fashion" },
    new CategoryItem { Id = "toys", Label = "Toys" }
};


    public class CategoryItem
{
    public string Id { get; set; }
    public string Label { get; set; }
}


    protected async Task PerformSearch()
    {
    //    if (string.IsNullOrWhiteSpace(searchText) && string.IsNullOrWhiteSpace(selectedCategory))
    //{
    //    Snackbar.Add("Please enter search text or select a category", Severity.Warning);
    //    return;
    //}

        loading = true;

     
    }
}
