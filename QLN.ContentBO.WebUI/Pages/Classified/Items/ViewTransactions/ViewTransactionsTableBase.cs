using Microsoft.AspNetCore.Components;
using System;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Components.AdHistoryDialog;
using System.Collections.Generic;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.ViewTransactions
{
    public partial class ViewTransactionsTableBase : ComponentBase
    {
        protected List<ListingItemsTransaction> Listings { get; set; } = new();
        [Inject] public IDialogService DialogService { get; set; }
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected int TotalCount => Listings.Count;
        protected void HandlePageChange(int newPage)
        {
            currentPage = newPage;
            StateHasChanged();
        }

        protected void HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            currentPage = 1; // reset to first page
            StateHasChanged();
        }


        protected override void OnInitialized()
        {
            Listings = GetSampleData();
        }
        protected string selectedTab = "paytopublish";
         private void OpenAdDialog(ListingItemsTransaction item)
        {
            int adId = int.TryParse(item.AdId, out var parsedId) ? parsedId : 0;

            var adHistoryList = new List<AdHistoryModel>();
            for (int i = 0; i < 8; i++)
            {
                adHistoryList.Add(new AdHistoryModel
                {
                    AdId = adId + i,
                    UsedOn = DateTime.Now.AddHours(-i)
                });
            }

            var parameters = new DialogParameters
            {
                { "AdHistoryList", adHistoryList }
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            DialogService.Show<AdHistoryDialog>("", parameters, options);
        }

       protected List<ToggleTabs.TabOption> tabOptions = new()
        {
            new() { Label = "Pay To Publish", Value = "paytopublish" },
            new() { Label = "Pay To Promote", Value = "paytopromote" },
            new() { Label = "Pay To Feature", Value = "paytofeature" },
            new() { Label = "Bulk Refresh", Value = "bulkrefresh" }
        };
         protected async Task OnTabChanged(string newTab)
        {
            selectedTab = newTab;

            int? status = newTab switch
            {
                "paytopublish" => 1,
                "paytopromote" => 2,
                "paytofeature" => 3,
                _ => null
            };

        }

       private List<ListingItemsTransaction> GetSampleData()
        {
            return new List<ListingItemsTransaction>
            {
                new() { AdId = "21435", OrderId = "21660", ProductType = "P2P", UserName = "Rashid", Status = "Active", Email = "2311", Mobile = "Electronics", WhatsApp = "Phone", Amount = "Apple", CreationDate = DateTime.Parse("2025-04-12"), PublishedDate = DateTime.Parse("2025-04-12"), StartDate = DateTime.Parse("2025-04-12"), EndDate = DateTime.Parse("2025-04-12") },
                new() { AdId = "21435", OrderId = "21435", ProductType = "P2F", UserName = "Rashid", Status = "Pending Approval", Email = "2315", Mobile = "Electronics", WhatsApp = "Phone", Amount = "Apple", CreationDate = DateTime.Parse("2025-04-12"), PublishedDate = DateTime.Parse("2025-04-12"), StartDate = DateTime.Parse("2025-04-12"), EndDate = DateTime.Parse("2025-04-12") },
                new() { AdId = "N/A", OrderId = "21342", ProductType = "Bulk Refresh", UserName = "Rashid", Status = "N/A", Email = "2315", Mobile = "Electronics", WhatsApp = "Phone", Amount = "Apple" },
                new() { AdId = "21435", OrderId = "23415", ProductType = "P2P", UserName = "Rashid", Status = "Need Changes", Email = "2315", Mobile = "Electronics", WhatsApp = "Phone", Amount = "Apple", CreationDate = DateTime.Parse("2025-04-12"), PublishedDate = DateTime.Parse("2025-04-12"), StartDate = DateTime.Parse("2025-04-12"), EndDate = DateTime.Parse("2025-04-12") }
            };
        }

        protected void OnPreview(ListingItemsTransaction item)
        {
            OpenAdDialog(item);
            Console.WriteLine($"Preview clicked: {item.AdId}");
        }

    }
}
