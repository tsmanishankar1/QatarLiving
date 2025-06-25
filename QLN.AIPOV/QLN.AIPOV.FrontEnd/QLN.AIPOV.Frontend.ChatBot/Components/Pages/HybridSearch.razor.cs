using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.AIPOV.Frontend.ChatBot.Services.Interfaces;

namespace QLN.AIPOV.Frontend.ChatBot.Components.Pages
{
    public partial class HybridSearch
    {
        [Inject] public required ISnackbar Snackbar { get; set; }
        [Inject] public required IJSRuntime JSRuntime { get; set; }
        [Inject] public required IAzureSearchService AzureSearchService { get; set; }
        [Inject] public required ISearchService SearchService { get; set; }

        private string SearchQuery { get; set; } = string.Empty;
        private string SearchType { get; set; } = "hybrid"; // Default to hybrid search
        private List<SearchDocument> SearchDocuments { get; set; } = new();
        private string IndexDiagnostics { get; set; } = string.Empty;
        private bool IsLoading { get; set; } = false;

        protected override Task OnInitializedAsync()
        {
            SearchType = "hybrid";
            return base.OnInitializedAsync();
        }

        private async Task PerformSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                SearchDocuments.Clear();
                return;
            }

            IsLoading = true;
            StateHasChanged();

            try
            {
                List<SearchDocument>? searchDocuments = null;

                // Use the appropriate search method based on selected type
                switch (SearchType)
                {
                    case "keyword":
                        searchDocuments = await SearchService.KeywordSearchAsync(SearchQuery);
                        break;
                    case "vector":
                        searchDocuments = await SearchService.VectorSearchAsync(SearchQuery);
                        break;
                    case "hybrid":
                    default:
                        searchDocuments = await SearchService.HybridSearchAsync(SearchQuery);
                        break;
                }

                if (searchDocuments == null)
                {
                    Snackbar.Add("No results found", Severity.Warning);
                    SearchDocuments.Clear();
                    return;
                }

                SearchDocuments = searchDocuments;

                // Log information about search results
                await JSRuntime.InvokeVoidAsync("console.log",
                    $"Found {SearchDocuments.Count} results using {SearchType} search");

                if (SearchDocuments.Count > 0)
                {
                    await JSRuntime.InvokeVoidAsync("console.log",
                        $"Fields in first document: {string.Join(", ", SearchDocuments[0].Keys)}");
                }
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("console.log",
                    $"Error performing search: {ex.GetBaseException().Message}");
                Snackbar.Add($"Error performing {SearchType} search: {ex.Message}", Severity.Error);
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private async Task HandleKeyPress(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await PerformSearchAsync();
            }
        }

        private bool IsCommonField(string fieldName)
        {
            string[] commonFields = {
                "title", "name", "content", "description", "id",
                "FirstName", "LastName", "Email", "MobileNumber", "Skills", "WorkExperience", "FullName"
            };
            return commonFields.Contains(fieldName.ToLower());
        }

        private async Task DiagnoseIndex()
        {
            IsLoading = true;
            StateHasChanged();

            try
            {
                IndexDiagnostics = await AzureSearchService.GetIndexDefinitionAsync();
            }
            catch (Exception ex)
            {
                IndexDiagnostics = $"Error diagnosing index: {ex.Message}";
                Snackbar.Add("Error diagnosing index", Severity.Error);
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private void SetSearchType(string searchType)
        {
            SearchType = searchType;
            JSRuntime.InvokeVoidAsync("console.log", $"SearchType changed to: {SearchType}");
            StateHasChanged();
        }
    }
}