using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;
using System.Text.Json;

public class ItemsComponentBase : ComponentBase
{
    [Inject] protected SearchStateService SearchState { get; set; }

    [Inject] private IClassifiedsServices _classifiedsService { get; set; } = default!;
    protected List<CategoryTreeDto> CategoryTrees { get; set; } = new();
    protected List<CategoryField> CategoryFilters { get; set; } = new();

    [Inject] private ILogger<ItemsComponentBase> Logger { get; set; } = default!;

    protected bool IsLoadingSearch { get; set; } = true;
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
                filters.Add("category", SearchState.ItemCategory);
            if (!string.IsNullOrWhiteSpace(SearchState.ItemBrand))
                filters.Add("brand", SearchState.ItemBrand);
            if (!string.IsNullOrWhiteSpace(SearchState.ItemSortBy))
                filters.Add("orderBy", SearchState.ItemSortBy);


            var payload = new Dictionary<string, object>
            {
                ["text"] = searchText ?? SearchState.ItemSearchText,
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
