using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Models;

public class CommunitySearchBarSectionBase : ComponentBase
{

    [Inject] protected ISnackbar Snackbar { get; set; }
    [Inject] protected ICommunityService CommunityService { get; set; }

    [Parameter] public EventCallback<Dictionary<string, object>> OnSearchCompleted { get; set; }
    [Parameter] public EventCallback<string> OnCategoryChanged { get; set; }




    protected string searchText;
    protected string selectedCategory;
    protected bool loading = false;

    protected string SelectedCategoryId { get; set; }

    protected List<SelectOption> CategorySelectOptions { get; set; }

    protected override async Task OnInitializedAsync()
    {

        try
        {
            CategorySelectOptions = await CommunityService.GetForumCategoriesAsync();
           
        }
        catch (Exception ex)
        {
            Snackbar.Add("Failed to load categories", Severity.Error);
            Console.WriteLine(ex.Message);
            CategorySelectOptions = new List<SelectOption>();
        }
    }


    protected async Task OnCategoryChange(string newId)
    {
        SelectedCategoryId = newId;
        await OnCategoryChanged.InvokeAsync(newId); 
    }

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
