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
    [Inject] protected NavigationManager NavigationManager { get; set; }

    [Parameter] public EventCallback<Dictionary<string, object>> OnSearchCompleted { get; set; }
    [Parameter] public EventCallback<string> OnCategoryChanged { get; set; }


    [Parameter]
    public string InitialCategoryId { get; set; }

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
            if (!string.IsNullOrEmpty(InitialCategoryId))
            {
                SelectedCategoryId = InitialCategoryId;
            }

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
        NavigationManager.NavigateTo($"content/community?categoryId={newId}", forceLoad: false);
    }

    protected override void OnParametersSet()
    {
        // Update selected category when parameter changes
        if (!string.IsNullOrEmpty(InitialCategoryId) && SelectedCategoryId != InitialCategoryId)
        {
            SelectedCategoryId = InitialCategoryId;
        }
    }

    protected async Task PerformSearch()
    {
        

       
            //var success = await CommunitySearchService.PerformSearchAsync(searchText);
            Snackbar.Add("More features are coming soon!", Severity.Success);

        
    }
  
}
