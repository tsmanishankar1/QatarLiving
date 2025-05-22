using Microsoft.AspNetCore.Components;
using MudBlazor;
using static QLN.Web.Shared.Helpers.HttpErrorHelper;

public class ServicesSearchBarSectionBase : ComponentBase
{
    protected string searchText;
    protected string selectedCategory;
    protected bool loading = false;

    protected List<string> categoryOptions = new()
    {
        "Mobile Phones & Tablets", "Accessories", "Fashion", "Toys"
    };

    protected async Task ServicesPerformSearch()
    {
    
    }
}
