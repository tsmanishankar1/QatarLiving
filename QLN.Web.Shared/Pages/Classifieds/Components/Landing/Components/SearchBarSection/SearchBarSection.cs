using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Web.Shared.Components.Classifieds.FeaturedItemCard;
using QLN.Web.Shared.Services;
using static QLN.Web.Shared.Helpers.HttpErrorHelper;

public class SearchBarSectionBase : ComponentBase
{
    [Inject] protected ISnackbar Snackbar { get; set; }
    [Inject] protected ApiService Api { get; set; }

    [Parameter] public EventCallback<List<FeaturedItemCard.FeaturedItem>> OnSearchCompleted { get; set; }

    protected string searchText;
    protected string selectedCategory;
    protected bool loading = false;

    protected List<string> categoryOptions = new()
    {
        "Electronics", "Accessories", "Fashion", "Toys"
    };

    protected async Task PerformSearch()
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            Snackbar.Add("Please enter search text", Severity.Warning);
            return;
        }

        loading = true;

        var payload = new
        {
            text = searchText,
            top = 0,
            filters = new { Category = selectedCategory },
            orderBy = "relevance"
        };

        try
        {
            var result = await Api.PostAsync<object, List<FeaturedItemCard.FeaturedItem>>("classifeds/search", payload);

            Snackbar.Add("Search successful", Severity.Success);
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
