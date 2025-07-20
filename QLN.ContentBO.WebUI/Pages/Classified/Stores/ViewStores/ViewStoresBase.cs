using Microsoft.AspNetCore.Components;
using System;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using System.Collections.Generic;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewStores
{
    public partial class ViewStoresBase : ComponentBase
    {
        public class SubscriptionOrder
        {
            public int OrderId { get; set; }
            public string CompanyName { get; set; }
            public string Email { get; set; }
            public string Mobile { get; set; }
            public string Whatsapp { get; set; }
            public string WebUrl { get; set; }
            public string Status { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }


        protected List<SubscriptionOrder> Listings { get; set; } = new();
        private SubscriptionOrder selectedOrder = new();
        protected HashSet<SubscriptionOrder> SelectedListings { get; set; } = new();
        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public IDialogService DialogService { get; set; }
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected int TotalCount => Listings.Count;
        protected string SearchTerm { get; set; } = string.Empty;
        protected bool Ascending = true;

        protected async Task HandleSearch(string searchTerm)
        {
            SearchTerm = searchTerm;
            Console.WriteLine($"Search triggered: {SearchTerm}");
            // Add logic to filter your listing data based on SearchTerm
        }

        protected async Task HandleSort(bool sortOption)
        {
            Ascending = sortOption;
            Console.WriteLine($"Sort triggered: {sortOption}");
            // Add logic to sort your listing data based on SortOption
        }
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
        protected string selectedTab = "pendingApproval";

        protected List<ToggleTabs.TabOption> tabOptions = new()
        {
            new() { Label = "Pending Approval", Value = "pendingApproval" },
            new() { Label = "Published", Value = "published" },
            new() { Label = "Unpublished", Value = "unpublished" },
            new() { Label = "P2P", Value = "p2p" },
            new() { Label = "Promoted", Value = "promoted" },
            new() { Label = "Featured", Value = "featured" }
        };
        protected async Task OnTabChanged(string newTab)
        {
            selectedTab = newTab;

            int? status = newTab switch
            {
                "published" => 1,
                "unpublished" => 2,
                "Promoted" => 3,
                _ => null
            };

        }
        protected async Task ShowConfirmation(string title, string description, string buttonTitle, Func<Task> onConfirmedAction)
        {
            var parameters = new DialogParameters
        {
            { "Title", title },
            { "Descrption", description },
            { "ButtonTitle", buttonTitle },
            { "OnConfirmed", EventCallback.Factory.Create(this, onConfirmedAction) }
        };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = DialogService.Show<ConfirmationDialog>("", parameters, options);
            var result = await dialog.Result;

        }
        private void OpenRejectDialog()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Reject Verification" },
                { "Description", "Please enter a reason before rejecting" },
                { "ButtonTitle", "Reject" },
                { "OnRejected", EventCallback.Factory.Create<string>(this, HandleRejection) }
            };
            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };
            var dialog = DialogService.Show<RejectVerificationDialog>("", parameters, options);
        }
        private List<SubscriptionOrder> GetSampleData()
        {
            DateTime commonDate = DateTime.Parse("2025-04-12");

            return new List<SubscriptionOrder>
    {
        new SubscriptionOrder
        {
            OrderId = 21435,
            CompanyName = "Rashid",
            Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Status = "Active",
            StartDate = commonDate,
            EndDate = commonDate
        },
        new SubscriptionOrder
        {
            OrderId = 21435,
            CompanyName = "Rashid",
            Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Status = "On Hold",
            StartDate = commonDate,
            EndDate = commonDate
        },
        new SubscriptionOrder
        {
            OrderId = 21342,
            CompanyName = "Rashid",
            Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Status = "Active",
            StartDate = commonDate,
            EndDate = commonDate
        },
        new SubscriptionOrder
        {
            OrderId = 23415,
            CompanyName = "Rashid",
            Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Status = "Cancelled",
            StartDate = commonDate,
            EndDate = commonDate
        }
    };
        }
        protected void OnEdit(ListingItem item)
        {
            Console.WriteLine($"Edit clicked: {item.AdTitle}");
        }

        protected void OnPreview(ListingItem item)
        {
            Console.WriteLine($"Preview clicked: {item.AdTitle}");
        }

        protected Task ApproveSelected() => Task.Run(() => Console.WriteLine("Approved Selected"));
        protected Task UnpublishSelected() => Task.Run(() => Console.WriteLine("Unpublished Selected"));
        protected Task PublishSelected() => Task.Run(() => Console.WriteLine("Published Selected"));
        protected Task RemoveSelected() => Task.Run(() => Console.WriteLine("Removed Selected"));
        protected Task UnpromoteSelected() => Task.Run(() => Console.WriteLine("Unpromoted Selected"));
        protected Task UnfeatureSelected() => Task.Run(() => Console.WriteLine("Unfeatured Selected"));

        protected Task Approve(ListingItem item) => Task.Run(() => Console.WriteLine($"Approved: {item.AdId}"));
        protected Task Publish(ListingItem item) => Task.Run(() => Console.WriteLine($"Published: {item.AdId}"));
        protected Task Unpublish(ListingItem item) => Task.Run(() => Console.WriteLine($"Unpublished: {item.AdId}"));
        protected Task OnRemove(ListingItem item) => Task.Run(() => Console.WriteLine($"Removed: {item.AdId}"));
        private void HandleRejection(string reason)
        {
            Console.WriteLine("Rejection Reason: " + reason);
            // Send to API or handle in state
        }
        protected Task RequestChanges(ListingItem item)
        {
            Console.WriteLine($"Requested changes for: {item.AdId}");
            OpenRejectDialog();
            return Task.CompletedTask;
        }
        protected void EditOrder(SubscriptionOrder order)
        {
            DateTime commonDate = DateTime.Parse("2025-04-12");
            selectedOrder = new SubscriptionOrder
            {
                OrderId = 21435,
                CompanyName = "Rashid",
                Email = "Rashid.r@gmail.com",
                Mobile = "+974 5030537",
                Whatsapp = "+974 5030537",
                WebUrl = "Rashidr.com",
                Status = "Active",
                StartDate = commonDate,
                EndDate = commonDate
            };

            Console.WriteLine($"Editing Order ID: {order.OrderId}");
        }
        protected void CancelOrder(SubscriptionOrder order)
        {
            order.Status = "Cancelled";
            Console.WriteLine($"Cancelled Order ID: {order.OrderId}");
        }
        protected void OnViewClicked(SubscriptionOrder store)
        {
            var name = "Rashid";
            NavigationManager.NavigateTo($"/manage/classified/stores/createform/{name}");
        }
      
    }
}
