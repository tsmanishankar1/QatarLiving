using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.ViewTransactions
{
    public partial class ViewTransactionsBase : ComponentBase
    {
        [Inject] public IClassifiedService ClassifiedService { get; set; }

        protected string SearchTerm { get; set; } = string.Empty;
        protected bool Ascending = true;
        protected bool IsLoading = true;
        protected DateTime? FilterCreated { get; set; }
        protected DateTime? FilterPublished { get; set; }
        protected DateTime? FilterStart { get; set; }
        protected DateTime? FilterEnd { get; set; }
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 12;
        protected string SelectedTab { get; set; } = "paytopublish";

        protected List<ItemViewTransaction> Transactions { get; set; } = new();
        protected int TotalRecords { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadTransactionsAsync();
        }

        protected async Task HandleTabChanged(string newTab)
        {
            SelectedTab = newTab;
            CurrentPage = 1; // reset to first page
            await LoadTransactionsAsync();
        }

        protected async Task HandleSearch(string searchTerm)
        {
            SearchTerm = searchTerm;
            await LoadTransactionsAsync();
        }

        protected async Task HandleSort(bool sortOption)
        {
            Ascending = sortOption;
            await LoadTransactionsAsync();
        }

        protected async Task HandlePageChanged(int newPage)
        {
            CurrentPage = newPage;
            await LoadTransactionsAsync();
        }

        protected async Task HandlePageSizeChanged(int newSize)
        {
            PageSize = newSize;
            CurrentPage = 1;
            await LoadTransactionsAsync();
        }
        protected async Task HandleDateFilterChanged((DateTime? created, DateTime? published, DateTime? start, DateTime? end) filters)
        {
            (FilterCreated, FilterPublished, FilterStart, FilterEnd) = filters;
            await LoadTransactionsAsync();
        }
        
        private async Task LoadTransactionsAsync()
        {
            try
            {
                IsLoading = true;
                var payload = new Dictionary<string, object>
                {
                    ["searchText"] = SearchTerm,
                    ["sortOrder"] = Ascending ? "desc" : "asc",
                    ["sortBy"] = "creationDate",
                    ["pageNumber"] = CurrentPage,
                    ["pageSize"] = PageSize,
                    ["transactionType"] = SelectedTab switch
                    {
                        "paytopublish" => "Pay To Publish",
                        "paytopromote" => "Pay To Promote",
                        "paytofeature" => "Pay To Feature",
                        "bulkrefresh" => "Bulk Refresh",
                        _ => ""
                    }
                };

                // Add filters if present
                if (FilterCreated.HasValue)
                    payload["dateCreated"] = FilterCreated.Value.ToString("yyyy-MM-dd");

                if (FilterPublished.HasValue)
                    payload["datePublished"] = FilterPublished.Value.ToString("yyyy-MM-dd");

                if (FilterStart.HasValue)
                    payload["dateStart"] = FilterStart.Value.ToString("yyyy-MM-dd");

                if (FilterEnd.HasValue)
                    payload["dateEnd"] = FilterEnd.Value.ToString("yyyy-MM-dd");

                var responses = await ClassifiedService.SearchClassifiedsViewTransactionAsync(payload);

                if (responses.Count > 0 && responses[0].IsSuccessStatusCode)
                {
                    var json = await responses[0].Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<PagedTransactionResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result != null)
                    {
                        Transactions = result.Records;
                        TotalRecords = result.TotalRecords;
                    }
                }
                else
                {
                    Console.WriteLine($"API call failed: {responses[0].StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading transactions: {ex.Message}");
            }
            finally
            {
              IsLoading = false;
            }
        }
    }
}
