using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Web.Shared.Models;

public class CommunitySearchBarSectionBase : ComponentBase
{

    [Inject] protected ISnackbar Snackbar { get; set; }
    [Parameter] public EventCallback<Dictionary<string, object>> OnSearchCompleted { get; set; }

   


    protected string searchText;
    protected string selectedCategory;
    protected bool loading = false;

    protected string SelectedCategoryId { get; set; }

    protected List<SelectOption> CategorySelectOptions { get; set; }

    protected override Task OnInitializedAsync()
    {
        CategorySelectOptions = new List<SelectOption>
    {
        new() { Id = "Qatar Living Lounge", Label = "Qatar Living Lounge" },
        new() { Id = "Advice & Help", Label = "Advice & Help" },
        new() { Id = "Welcome to Qatar", Label = "Welcome to Qatar" },
        new() { Id = "Visas and Permits", Label = "Visas and Permits" },
        new() { Id = "Motoring", Label = "Motoring" },
        new() { Id = "Qatari Culture", Label = "Qatari Culture" },
        new() { Id = "Ramadan & Eid", Label = "Ramadan & Eid" },
        new() { Id = "Parent's Corner", Label = "Parent's Corner" },
        new() { Id = "Missing home!", Label = "Missing home!" },
        new() { Id = "Salary & Allowances", Label = "Salary & Allowances" },
        new() { Id = "Business & Finance", Label = "Business & Finance" },
        new() { Id = "Education", Label = "Education" },
        new() { Id = "Dining & Restaurants", Label = "Dining & Restaurants" },
        new() { Id = "Electronics & Gadgets", Label = "Electronics & Gadgets" },
        new() { Id = "Technology & Internet", Label = "Technology & Internet" },
        new() { Id = "Fashion & Beauty", Label = "Fashion & Beauty" },
        new() { Id = "Environment", Label = "Environment" },
        new() { Id = "Pets & Animals", Label = "Pets & Animals" },
        new() { Id = "Movies & Cinemas", Label = "Movies & Cinemas" },
        new() { Id = "Travel and Tourism", Label = "Travel and Tourism" }
    };

        return Task.CompletedTask;
    }

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

        await OnSearchCompleted.InvokeAsync(payload);

    }
}
