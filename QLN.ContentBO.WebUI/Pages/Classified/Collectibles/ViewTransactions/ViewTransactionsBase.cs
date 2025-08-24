using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Classified.Collectibles.ViewTransactions
{
    public partial class ViewTransactionsBase : ComponentBase
    {
        [Inject] public ICollectiblesService CollectiblesService { get; set; }

        protected string? SearchTerm { get; set; }
        protected bool Ascending = true;
        protected bool IsLoading = true;
        protected DateTime? FilterCreated { get; set; }
        protected DateTime? FilterPublished { get; set; }
        protected DateTime? FilterStart { get; set; }
        protected DateTime? FilterEnd { get; set; }
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 12;
        protected string SelectedTab { get; set; } = "paytopublish";

        protected List<ItemTransactionItem> Transactions { get; set; } = new();
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
                var request = new ItemTransactionRequest
                {
                    SubVertical = (int)SubVerticalTypeEnum.Collectibles,
                    Status = "Active",
                    DateCreated = FilterCreated?.Date.ToString("yyyy-MM-dd"),
                    DatePublished = FilterPublished?.Date.ToString("yyyy-MM-dd"),
                    DateStart = FilterStart?.Date.ToString("yyyy-MM-dd") ,
                    DateEnd = FilterEnd?.Date.ToString("yyyy-MM-dd"),
                    PageNumber = CurrentPage,
                    PageSize = PageSize,
                    SearchText = SearchTerm,
                    ProductType = SelectedTab switch
                    {
                        "paytopublish" => "Pay To Publish",
                        "paytopromote" => "Pay To Promote",
                        "paytofeature" => "Pay To Feature",
                        "bulkrefresh" => "Bulk Refresh",
                        _ => ""
                    },
                    PaymentMethod = "",
                    SortBy = "creationDate",
                    SortOrder = Ascending ? "desc" : "asc"
                };
                var response = await CollectiblesService.GetTransactionListing(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ItemTransactionResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result != null)
                    {
                        Transactions = result.Records;
                        TotalRecords = result.TotalRecords;
                    }
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
