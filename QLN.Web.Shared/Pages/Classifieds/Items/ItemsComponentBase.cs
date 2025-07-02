using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;
using System.Text.Json;
using MudBlazor;
public class ItemsComponentBase : ComponentBase
{
    [Inject] protected SearchStateService SearchState { get; set; }

    [Inject] private IClassifiedsServices _classifiedsService { get; set; } = default!;
    protected List<CategoryTreeDto> CategoryTrees { get; set; } = new();
    protected List<CategoryField> CategoryFilters { get; set; } = new();
    [Inject] public ISnackbar Snackbar { get; set; }

    [Inject] private ILogger<ItemsComponentBase> Logger { get; set; } = default!;

    protected bool IsLoadingSearch { get; set; } = true;

    protected bool IsLoadingSaveSearch { get; set; } = false;
    protected bool IsSaveSearch { get; set; } = false;
    protected bool IsLoadingCategories { get; set; } = true;

    protected string? ErrorMessage { get; set; }

    protected List<ClassifiedsIndex> SearchResults { get; set; } = new();
    protected void HandleViewModeChange(string newMode)
    {
        SearchState.ItemViewMode = newMode;
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
    protected async Task OnCategoryChanged(string categoryName)
    {
        // Find category by name (search recursively)
        var selectedCategory = FindCategoryByName(CategoryTrees, categoryName);

        if (selectedCategory is not null)
        {

            await LoadCategoryFiltersAsync("items", selectedCategory.Id);
        }
        }
    private CategoryTreeDto? FindCategoryByName(IEnumerable<CategoryTreeDto> categories, string name)
    {
        foreach (var category in categories)
        {
            if (category.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return category;
            }

            var foundInChildren = FindCategoryByName(category.Children, name);
            if (foundInChildren != null)
            {
                return foundInChildren;
            }
        }

        return null;
    }

    private async Task LoadCategoryTreesAsync()
    {
        if (!string.IsNullOrWhiteSpace(SearchState.ItemCategory))
        {
            IsLoadingCategories = false;
            return;
        }
        try
        {
            var response = await _classifiedsService.GetAllCategoryTreesAsync("Items");

            if (response is { IsSuccessStatusCode: true })
            {
                var result = await response.Content.ReadFromJsonAsync<List<CategoryTreeDto>>();
                CategoryTrees = result ?? new();
                SearchState.ItemCategoryTrees = CategoryTrees;
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
            { "SubVertical", "Items" }
        };

            // Include price filters only if they are set
            if (SearchState.ItemMinPrice.HasValue)
                filters.Add("minPrice", SearchState.ItemMinPrice.Value);
            if (SearchState.ItemMaxPrice.HasValue)
                filters.Add("maxPrice", SearchState.ItemMaxPrice.Value);
            if (!string.IsNullOrWhiteSpace(SearchState.ItemCategory))
                filters.Add("CategoryId", SearchState.ItemCategory);
            if (!string.IsNullOrWhiteSpace(SearchState.ItemSubCategory))
                filters.Add("L1CategoryId", SearchState.ItemSubCategory);
            if (!string.IsNullOrWhiteSpace(SearchState.ItemSubSubCategory))
                filters.Add("L2CategoryId", SearchState.ItemSubSubCategory);
            if (!string.IsNullOrWhiteSpace(SearchState.ItemBrand))
                filters.Add("brand", SearchState.ItemBrand);
          if (SearchState.ItemHasWarrantyCertificate)
            {
                filters["hasWarrantyCertificate"] = SearchState.ItemHasWarrantyCertificate;
            }


            foreach (var fieldFilter in SearchState.ItemFieldFilters)
            {
                if (fieldFilter.Value?.Any() == true)
                {
                    filters[fieldFilter.Key] = fieldFilter.Value;
                }
            }
            var payload = new Dictionary<string, object>
            {
                ["text"] = searchText ?? SearchState.ItemSearchText,
                ["orderBy"] = SearchState.ItemSortBy,
                ["filters"] = filters,    
            };

            var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
           // Logger.LogInformation("Sending search payload: {Payload}", payloadJson);

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
    protected async Task SaveSearchAsync(string searchName)
    {
        IsLoadingSaveSearch = true;
        
        try
        {
            if (string.IsNullOrWhiteSpace(SearchState.ItemSearchText))
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
            Logger.LogInformation("Saving search with payload: {Payload}", System.Text.Json.JsonSerializer.Serialize(savePayload));
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
                    { "SubVertical", "Items" }
                };

            if (SearchState.ItemMinPrice.HasValue)
                filters["minPrice"] = SearchState.ItemMinPrice.Value;

            if (SearchState.ItemMaxPrice.HasValue)
                filters["maxPrice"] = SearchState.ItemMaxPrice.Value;

            if (!string.IsNullOrWhiteSpace(SearchState.ItemCategory))
                filters["CategoryId"] = SearchState.ItemCategory;

            if (!string.IsNullOrWhiteSpace(SearchState.ItemSubCategory))
                filters["L1CategoryId"] = SearchState.ItemSubCategory;

            if (!string.IsNullOrWhiteSpace(SearchState.ItemSubSubCategory))
                filters["L2CategoryId"] = SearchState.ItemSubSubCategory;

            if (!string.IsNullOrWhiteSpace(SearchState.ItemBrand))
                filters["brand"] = SearchState.ItemBrand;

            if (SearchState.ItemHasWarrantyCertificate)
                filters["hasWarrantyCertificate"] = SearchState.ItemHasWarrantyCertificate;

            foreach (var fieldFilter in SearchState.ItemFieldFilters)
            {
                if (fieldFilter.Value?.Any() == true)
                    filters[fieldFilter.Key] = fieldFilter.Value;
            }

            var safeText = string.IsNullOrWhiteSpace(searchText) 
                ? (string.IsNullOrWhiteSpace(SearchState.ItemSearchText) ? "" : SearchState.ItemSearchText) 
                : searchText;


            return new Dictionary<string, object>
        {
            ["text"] = safeText,
            ["orderBy"] = SearchState.ItemSortBy ?? "relevance",
            ["filters"] = filters
        };
        }

    private async Task LoadCategoryFiltersAsync(string vertical, Guid mainCategoryId)
    {
        try
        {
            var response = await _classifiedsService.GetCategoryFiltersAsync(vertical, mainCategoryId);

            if (response is { IsSuccessStatusCode: true })
            {
                var filters = await response.Content.ReadFromJsonAsync<List<CategoryField>>();
                CategoryFilters = filters ?? new();
                SearchState.ItemCategoryFilters = CategoryFilters;
            }
            else
            {
                ErrorMessage = $"Failed to load category filters. Status: {response?.StatusCode}";
                CategoryFilters = new();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error loading category filters.";
            Logger.LogError(ex, ErrorMessage);
            CategoryFilters = new();
        }
    }

}
