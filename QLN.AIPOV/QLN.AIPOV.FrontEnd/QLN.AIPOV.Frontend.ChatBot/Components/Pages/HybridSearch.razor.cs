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
        [Inject] public required IAzureSearchService SearchService { get; set; } // Added the Azure AI Search Service

        private string SearchQuery { get; set; } = string.Empty;
        private List<SearchDocument> SearchResults { get; set; } = [];
        private string IndexDiagnostics { get; set; } = string.Empty;

        private bool IsLoading { get; set; } = false;

        private async Task PerformSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                SearchResults.Clear();
                return;
            }

            IsLoading = true;
            StateHasChanged();

            try
            {
                var searchResponse = await SearchService.SearchDocumentsHybridAsync(SearchQuery);
                SearchResults = searchResponse.ToList();
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("console.log",
                    @$"Error performing search. {ex.GetBaseException().Message}");
                Snackbar.Add("Error performing search", Severity.Error);
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
            string[] commonFields = { "title", "name", "content", "description", "id" };
            return commonFields.Contains(fieldName.ToLower());
        }

        private async Task DiagnoseIndex()
        {
            IndexDiagnostics = await SearchService.GetIndexDefinitionAsync();
            StateHasChanged();
        }
    }
}
