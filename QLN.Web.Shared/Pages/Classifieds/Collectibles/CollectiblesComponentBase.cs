using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;
using System.Text.Json;
using MudBlazor;
public class CollectiblesComponentBase : ComponentBase
{
    [Inject] protected SearchStateService SearchState { get; set; }
    [Inject] private IClassifiedsServices _classifiedsService { get; set; } = default!;
    protected List<CategoryTreeDto> CategoryTrees { get; set; } = new();
    [Inject] private ILogger<CollectiblesComponentBase> Logger { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; }

    protected bool IsLoadingSaveSearch { get; set; } = false;
    protected int TotalCount { get; set; } = 0;
    protected bool IsLoadingSearch { get; set; } = true;
    protected bool IsLoadingCategories { get; set; } = true;
    protected bool IsSaveSearch { get; set; } = false;
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
            if (!string.IsNullOrWhiteSpace(SearchState.CollectiblesCondition))
                filters.Add("condition", SearchState.CollectiblesCondition);
            if (!string.IsNullOrWhiteSpace(SearchState.CollectiblesCategory))
                filters.Add("CategoryId", SearchState.CollectiblesCategory);
            if (!string.IsNullOrWhiteSpace(SearchState.CollectiblesSubCategory))
                filters.Add("L1CategoryId", SearchState.CollectiblesSubCategory);
            if (!string.IsNullOrWhiteSpace(SearchState.CollectiblesSubSubCategory))
                filters.Add("L2CategoryId", SearchState.CollectiblesSubSubCategory);
            if (SearchState.CollectiblesHasAuthenticityCertificate)
            {
                filters["HasAuthenticityCertificate"] = SearchState.CollectiblesHasAuthenticityCertificate;
            }


            foreach (var fieldFilter in SearchState.CollectiblesFilters)
            {
                if (fieldFilter.Value?.Any() == true)
                {
                    filters[fieldFilter.Key] = fieldFilter.Value;
                }
            }
            var payload = new Dictionary<string, object>
            {
                ["text"] = searchText ?? SearchState.CollectiblesSearchText,
                ["orderBy"] = SearchState.CollectiblesSortBy,
                ["filters"] = filters,
                ["pageNumber"] = SearchState.CollectiblesCurrentPage,
                ["pageSize"] = SearchState.CollectiblesPageSize
            };

            var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

            var responses = await _classifiedsService.SearchClassifiedsAsync(payload);
            var firstResponse = responses.FirstOrDefault();

            if (firstResponse is { IsSuccessStatusCode: true })
            {
                var result = await firstResponse.Content.ReadFromJsonAsync<ClassifiedsSearchResponse>();
                SearchResults = result?.ClassifiedsItems ?? new();
                TotalCount = result?.TotalCount ?? 0;
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

    protected async Task SaveSearchAsync(string searchName)
    {
        IsLoadingSaveSearch = true;
        try
        {
              if (string.IsNullOrWhiteSpace(SearchState.CollectiblesSearchText))
            {
                Snackbar.Add("Search text is required before saving.", Severity.Warning);
                return;
            }
                var searchQuery = BuildSearchPayload();
                bool hasAppliedAnything = false;
                if (searchQuery.TryGetValue("text", out var textVal) &&
                textVal is string text && !string.IsNullOrWhiteSpace(text))
            {
                hasAppliedAnything = true;
            }
        
            if (searchQuery.TryGetValue("filters", out var filtersObj) &&
                    filtersObj is Dictionary<string, object> filters)
                {
                    // Count filters that are not just "SubVertical"
                    var nonDefaultFilters = filters
                        .Where(f => !string.Equals(f.Key, "SubVertical", StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(f => f.Key, f => f.Value);

                    if (nonDefaultFilters.Count > 0)
                    {
                        hasAppliedAnything = true;
                    }
                }

            if (!hasAppliedAnything)
            {
                Snackbar.Add("Please apply a filter or enter search text before saving the search.", Severity.Warning);
                return;
            }
            var savePayload = new Dictionary<string, object>
            {
                ["name"] = searchName,
                ["searchQuery"] = searchQuery
            };

            var response = await _classifiedsService.PostClassifiedSaveSearchAsync(savePayload);

            if (response is { IsSuccessStatusCode: true })
            {
                IsSaveSearch = true;
                Snackbar.Add("Search saved successfully.", Severity.Success);
            }
            else
            {
                var error = await response?.Content.ReadAsStringAsync() ?? "Unknown error";
                Snackbar.Add($"Failed to save search: {error}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add("An error occurred while saving the search.", Severity.Error);
            Console.WriteLine($"Exception in SaveSearchAsync: {ex.Message}");
        }
        finally
        {
            IsLoadingSaveSearch = false;
            StateHasChanged();
        }
    }
    private Dictionary<string, object> BuildSearchPayload(string? searchText = null)
    {
         var filters = new Dictionary<string, object>
            {
                { "SubVertical", "Collectibles" }
            };
        if (SearchState.CollectiblesMinPrice.HasValue)
            filters["minPrice"] = SearchState.CollectiblesMinPrice.Value;

        if (SearchState.CollectiblesMaxPrice.HasValue)
            filters["maxPrice"] = SearchState.CollectiblesMaxPrice.Value;

        if (!string.IsNullOrWhiteSpace(SearchState.CollectiblesCondition))
            filters["condition"] = SearchState.CollectiblesCondition;

        if (!string.IsNullOrWhiteSpace(SearchState.CollectiblesCategory))
            filters["CategoryId"] = SearchState.CollectiblesCategory;

        if (!string.IsNullOrWhiteSpace(SearchState.CollectiblesSubCategory))
            filters["L1CategoryId"] = SearchState.CollectiblesSubCategory;

        if (!string.IsNullOrWhiteSpace(SearchState.CollectiblesSubSubCategory))
            filters["L2CategoryId"] = SearchState.CollectiblesSubSubCategory;

        if (SearchState.CollectiblesHasAuthenticityCertificate)
            filters["HasAuthenticityCertificate"] = SearchState.CollectiblesHasAuthenticityCertificate;

        foreach (var fieldFilter in SearchState.CollectiblesFilters)
        {
            if (fieldFilter.Value?.Any() == true)
                filters[fieldFilter.Key] = fieldFilter.Value;
        }

        var safeText = string.IsNullOrWhiteSpace(searchText) 
                ? (string.IsNullOrWhiteSpace(SearchState.CollectiblesSearchText) ? "" : SearchState.CollectiblesSearchText) 
                : searchText;
        return new Dictionary<string, object>
        {
            ["text"] = safeText,
            ["orderBy"] = SearchState.CollectiblesSortBy ?? "relevance",
            ["filters"] = filters,
            ["pageNumber"] = SearchState.CollectiblesCurrentPage,
            ["pageSize"] = SearchState.CollectiblesPageSize
        };
    }

}
