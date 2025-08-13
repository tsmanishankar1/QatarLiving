using Microsoft.AspNetCore.Components;
using System;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Components.AdHistoryDialog;
using System.Collections.Generic;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.Classified.Collectibles.ViewTransactions
{
    public partial class ViewTransactionsTableBase : ComponentBase
    {
        [Parameter] public bool IsLoading { get; set; }
        [Parameter] public List<ItemViewTransaction> Transactions { get; set; } = new();
        [Parameter] public int TotalRecords { get; set; }
        [Inject] public IDialogService DialogService { get; set; }
        public string SelectedTab { get; set; } = "paytopublish";
        [Parameter] public EventCallback<string> OnTabChanged { get; set; }
        [Parameter] public EventCallback<int> OnPageChanged { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChanged { get; set; }
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected async void HandlePageChange(int newPage)
        {
            currentPage = newPage;
            await OnPageChanged.InvokeAsync(currentPage);
        }

        protected async void HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            currentPage = 1;
            await OnPageSizeChanged.InvokeAsync(pageSize);
        }
         private void OpenAdDialog(ItemViewTransaction item)
        {
            int adId = int.TryParse(item.AdId, out var parsedId) ? parsedId : 0;

            var adHistoryList = new List<AdHistoryModel>();
            for (int i = 0; i < 20; i++)
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

        protected string GetTabTitle()
        {
            return SelectedTab switch
            {
                "paytopublish" => "Pay To Publish",
                "paytopromote" => "Pay To Promote",
                "paytofeature" => "Pay To Feature",
                "bulkrefresh" => "Bulk Refresh",
                _ => "Classified Items"
            };
        }
         protected async Task OnTabChangedInternal(string newTab)
        {
            SelectedTab = newTab;
            await OnTabChanged.InvokeAsync(newTab);
        }

     
        protected void OnPreview(ItemViewTransaction item)
        {
            OpenAdDialog(item);
            Console.WriteLine($"Preview clicked: {item.AdId}");
        }

    }
}
