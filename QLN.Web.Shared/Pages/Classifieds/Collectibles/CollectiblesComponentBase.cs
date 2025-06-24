using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;
using System.Text.Json;

public class CollectiblesComponentBase : ComponentBase
{
    [Inject] protected SearchStateService SearchState { get; set; }
    [Inject] private IClassifiedsServices _classifiedsService { get; set; } = default!;
    protected List<CategoryTreeDto> CategoryTrees { get; set; } = new();
    [Inject] private ILogger<CollectiblesComponentBase> Logger { get; set; } = default!;

    protected bool IsLoadingSearch { get; set; } = true;
    protected bool IsLoadingCategories { get; set; } = true;

    protected string? ErrorMessage { get; set; }

    protected List<ClassifiedsIndex> SearchResults { get; set; } = new();
    protected void HandleViewModeChange(string newMode)
    {
        SearchState.CollectiblesViewMode = newMode;
    }
     protected void ClearSearch()
    {
        // Example reset logic:
        SearchResults.Clear();
        StateHasChanged();

    }
    protected async Task OnSearchTriggered(string searchText)
{
    await LoadSearchResultsAsync(searchText);
}

    protected override async Task OnInitializedAsync()
    {
        await LoadCategoryTreesAsync();
  
        await LoadSearchResultsAsync();    
    }

    private async Task LoadCategoryTreesAsync()
    {
        if (!string.IsNullOrWhiteSpace(SearchState.CollectiblesCategory))
        {
            IsLoadingCategories = false;
            return;
        }
        try
        {
            var response = await _classifiedsService.GetAllCategoryTreesAsync("Collectibles");

            if (response is { IsSuccessStatusCode: true })
            {
                var result = await response.Content.ReadFromJsonAsync<List<CategoryTreeDto>>();
                CategoryTrees = result ?? new();
                 SearchState.CollectiblesCategoryTrees = CategoryTrees;
            }
            else
            {
                ErrorMessage = $"Failed to load category trees. Status: {response?.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error loading category trees.";
            Logger.LogError(ex, ErrorMessage);
        }
        finally
        {
            IsLoadingCategories = false;
        }
    }

    private async Task LoadSearchResultsAsync(string? searchText = null)
    {
        IsLoadingSearch = true;
        try
        {
            var filters = new Dictionary<string, object>
        {
            { "SubVertical", "Collectibles" }
        };

            // Include price filters only if they are set
            if (SearchState.CollectiblesMinPrice.HasValue)
                filters.Add("minPrice", SearchState.CollectiblesMinPrice.Value);
            if (SearchState.CollectiblesMaxPrice.HasValue)
                filters.Add("maxPrice", SearchState.CollectiblesMaxPrice.Value);
            if (!string.IsNullOrWhiteSpace(SearchState.CollectiblesCategory))
                filters.Add("category", SearchState.CollectiblesCategory);
            if (!string.IsNullOrWhiteSpace(SearchState.CollectiblesCondition))
                filters.Add("condition", SearchState.CollectiblesCondition);
            if (!string.IsNullOrWhiteSpace(SearchState.CollectiblesSortBy))
                filters.Add("orderBy", SearchState.CollectiblesSortBy);


            var payload = new Dictionary<string, object>
            {
                ["text"] = searchText ?? SearchState.CollectiblesSearchText,
                ["filters"] = filters
            };

            var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

            var responses = await _classifiedsService.SearchClassifiedsAsync(payload);
            var firstResponse = responses.FirstOrDefault();

            if (firstResponse is { IsSuccessStatusCode: true })
            {
                var result = await firstResponse.Content.ReadFromJsonAsync<ClassifiedsSearchResponse>();
                SearchResults = result?.ClassifiedsItems ?? new();
            }
            else
            {
                ErrorMessage = "Search failed or returned empty.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error loading classifieds.";
        }
        finally
        {
            IsLoadingSearch = false;
        }
    }
}
