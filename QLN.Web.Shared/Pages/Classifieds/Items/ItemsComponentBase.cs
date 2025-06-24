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
    [Inject] private ILogger<ItemsComponentBase> Logger { get; set; } = default!;

    protected bool IsLoadingSearch { get; set; } = true;
    protected bool IsLoadingCategories { get; set; } = true;

    protected string? ErrorMessage { get; set; }

    protected List<ClassifiedsIndex> SearchResults { get; set; } = new();
    protected List<CategoryTreeDto> CategoryTrees { get; set; } = new();

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
    protected override async Task OnInitializedAsync()
    {
        await LoadCategoryTreesAsync();
        await LoadSearchResultsAsync();
    }

    private async Task LoadCategoryTreesAsync()
    {
        try
        {
            var response = await _classifiedsService.GetAllCategoryTreesAsync("Items");

            if (response is { IsSuccessStatusCode: true })
            {
                var result = await response.Content.ReadFromJsonAsync<List<CategoryTreeDto>>();
                CategoryTrees = result ?? new();
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

    private async Task LoadSearchResultsAsync()
    {
        try
        {
            var payload = new Dictionary<string, object>
            {
                ["filters"] = new Dictionary<string, string>
                {
                    { "SubVertical", "Items" }
                }
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
