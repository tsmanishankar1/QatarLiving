using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Web.Shared.Components.Classifieds.FeaturedItemCard;
using QLN.Web.Shared.Services;
using static QLN.Web.Shared.Helpers.HttpErrorHelper;

public class CommunitySearchBarSectionBase : ComponentBase
{
    [Inject] protected ISnackbar Snackbar { get; set; }
    [Inject] protected ApiService Api { get; set; }

    [Parameter] public EventCallback<List<FeaturedItemCard.FeaturedItem>> OnSearchCompleted { get; set; }

    protected string searchText;
    protected string selectedCategory;
    protected bool loading = false;

    protected List<string> categoryOptions = new()
    {
         "Qatar Living Lounge",
    "Advice & Help",
    "Welcome to Qatar",
    "Visas and Permits",
    "Motoring",
    "Qatari Culture",
    "Ramadan & Eid",
    "Parent's Corner",
    "Missing home!",
    "Salary & Allowances",
    "Business & Finance",
    "Education",
    "Dining & Restaurants",
    "Electronics & Gadgets",
    "Technology & Internet",
    "Fashion & Beauty",
    "Environment",
    "Pets & Animals",
    "Movies & Cinemas",
    "Travel and Tourism"
    };

    protected async Task PerformSearch()
    {
   

        loading = true;

       var payload = new Dictionary<string, object>
    {
        ["text"] = searchText
    };

    if (!string.IsNullOrWhiteSpace(selectedCategory))
    {
        payload["filters"] = new Dictionary<string, object>
        {
            ["Category"] = selectedCategory
        };
    }

        try
        {
            var result = await Api.PostAsync<object, List<FeaturedItemCard.FeaturedItem>>("api/classified/search", payload);

            await OnSearchCompleted.InvokeAsync(result);
        }
        catch (HttpRequestException ex)
        {
            HandleHttpException(ex, Snackbar);
        }
        finally
        {
            loading = false;
        }
    }
}
