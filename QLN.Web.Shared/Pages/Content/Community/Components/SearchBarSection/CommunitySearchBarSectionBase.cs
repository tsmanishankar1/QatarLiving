using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;

public class CommunitySearchBarSectionBase : ComponentBase
{

    [Inject] protected ISnackbar Snackbar { get; set; }
    [Inject] protected ICommunityService CommunityService { get; set; }
    [Inject] protected ISearchService CommunitySearchService { get; set; }
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
        

        if (!string.IsNullOrEmpty(searchText))
        {
            //var success = await CommunitySearchService.PerformSearchAsync(searchText);
            Snackbar.Add("More features are coming soon!", Severity.Success);

        }
        else
        {
            Snackbar.Add("Please enter text to search", Severity.Warning);
        }
    }
  
}
