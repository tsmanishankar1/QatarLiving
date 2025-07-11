using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;

public class CommunitySearchBarSectionBaseV2 : ComponentBase
{

    [Inject] protected ISnackbar Snackbar { get; set; }
    [Inject] protected ICommunityService CommunityService { get; set; }
    [Inject] protected ISearchService CommunitySearchService { get; set; }
    [Inject] protected NavigationManager NavigationManager { get; set; }

    [Parameter] public EventCallback<Dictionary<string, object>> OnSearchCompleted { get; set; }
    [Parameter] public EventCallback<string> OnCategoryChanged { get; set; }
    [Parameter] public EventCallback<string> OnSearchTextChanged { get; set; }

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
            //Commented Drupal Category Service by jaswanth
            //CategorySelectOptions = await CommunityService.GetForumCategoriesAsync();

            CategorySelectOptions = (await CommunityService.GetCommunityCategoriesAsync())
     .Select(c => new SelectOption
     {
         Id = c.Id,
         Label = c.Name
     }).ToList();


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
        NavigationManager.NavigateTo($"content/v2/community?categoryId={newId}", forceLoad: false); // leaving this as it is a V2 component
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
        Console.WriteLine($"Search text submitted: {searchText}");
        await OnSearchTextChanged.InvokeAsync(searchText);
    }
    protected async Task ClearFilters()
    {
        searchText = string.Empty;
        SelectedCategoryId = null;

      

        NavigationManager.NavigateTo("content/community/v2", forceLoad: true);
    }


}
