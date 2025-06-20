using Microsoft.AspNetCore.Components;
using QLN.Common.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;

public class CollectiblesComponentBase : ComponentBase
{
    [Inject] private IClassifiedsServices _classifiedsService { get; set; } = default!;
    protected string currentViewMode = "grid";
    protected bool IsLoading { get; set; } = true;
    protected string? ErrorMessage { get; set; }

    protected List<ClassifiedsIndex> SearchResults { get; set; } = new();

    protected void HandleViewModeChange(string newMode)
    {
        currentViewMode = newMode;
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
                var payload = new Dictionary<string, object>
        {
            ["filters"] = new Dictionary<string, string>
            {
                { "SubVertical", "Collectibles" }
            }
        };

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
            Console.WriteLine("Search Init Error: " + ex);
            ErrorMessage = "Error loading classifieds.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
