using Markdig.Parsers;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewSubscriptionListing
{
    public partial class ViewSubscriptionListingBase : ComponentBase
    {
        [Inject] public IStoresService StoresService { get; set; } = default!;
        [Inject] public IDialogService DialogService { get; set; }
        [Inject] public ILogger<ViewSubscriptionListingBase> Logger { get; set; }
        protected List<StoreSubscriptionItem> Listings { get; set; } = new();

        protected int currentPage = 1;
        protected int pageSize = 12;
        protected int TotalCount { get; set; }
        protected string SearchTerm { get; set; } = string.Empty;
        protected bool Ascending = true;

        protected async Task HandleSearch(string searchTerm)
        {
            currentPage = 1;
            pageSize = 12;
            SearchTerm = searchTerm;
            var query = new StoreSubscriptionQuery
            {
                SubscriptionType = "",
                FilterDate = "",
                Page = currentPage,
                PageSize = pageSize,
                Search = searchTerm
            };

            var storesSubscriptionResponse = await GetStoreSubscriptionsAsync(query);
            Listings = storesSubscriptionResponse?.Items ?? [];
            TotalCount = storesSubscriptionResponse?.TotalCount ?? 0;
        }

        protected async Task HandleSort(bool sortOption)
        {
            Ascending = sortOption;
            Console.WriteLine($"Sort triggered: {sortOption}");
            // Add logic to sort your listing data based on SortOption
        }
        protected async Task HandlePageChange(int newPage)
        {
            currentPage = newPage;
            var query = new StoreSubscriptionQuery
            {
                SubscriptionType = "",
                FilterDate = "",
                Page = currentPage,
                PageSize = pageSize,
                Search = ""
            };

            var storesSubscriptionResponse = await GetStoreSubscriptionsAsync(query);
            Listings = storesSubscriptionResponse?.Items ?? [];
            TotalCount = storesSubscriptionResponse?.TotalCount ?? 0;
        }

        protected async Task HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            currentPage = 1; // reset to first page
            var query = new StoreSubscriptionQuery
            {
                SubscriptionType = "",
                FilterDate = "",
                Page = currentPage,
                PageSize = pageSize,
                Search = ""
            };

            var storesSubscriptionResponse = await GetStoreSubscriptionsAsync(query);
            Listings = storesSubscriptionResponse?.Items ?? [];
            TotalCount = storesSubscriptionResponse?.TotalCount ?? 0;
        }

        protected async override Task OnInitializedAsync()
        {
            var storesSubscriptionResponse = await LoadStoresSubscriptions();
            Listings = storesSubscriptionResponse?.Items ?? [];
            TotalCount = storesSubscriptionResponse?.TotalCount ?? 0;
        }

        protected void EditOrder(ViewSubscriptionListingDto order)
        {

            // You can open a dialog or set a flag to show a UI panel
            Console.WriteLine($"Editing Order ID: {order.OrderId}");
        }
        protected void CancelOrder(ViewSubscriptionListingDto order)
        {
            order.Status = "Cancelled";
            Console.WriteLine($"Cancelled Order ID: {order.OrderId}");
        }

        private async Task<StoreSubscriptionResponse> LoadStoresSubscriptions()
        {
            try
            {
                var query = new StoreSubscriptionQuery
                {
                    SubscriptionType = "",
                    FilterDate = "",
                    Page = currentPage,
                    PageSize = 12,
                    Search = ""
                };
                return await GetStoreSubscriptionsAsync(query);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadStoresSubscriptions");
                return new();
            }
        }

        private async Task<StoreSubscriptionResponse> GetStoreSubscriptionsAsync(StoreSubscriptionQuery storeSubscriptionQuery)
        {
            try
            {
                var apiResponse = await StoresService.GetAllStoresSubscription(storeSubscriptionQuery);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var storeSubscriptionResponse = await apiResponse.Content.ReadFromJsonAsync<StoreSubscriptionResponse>();

                    return storeSubscriptionResponse ?? new();
                }

                return new();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetStoreSubscriptionsAsync");
                return new();
            }
        }
    }
}
