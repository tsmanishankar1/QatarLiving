using Microsoft.AspNetCore.Components;
using System;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using System.Collections.Generic;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewSubscriptionListing
{
    public partial class ViewSubscriptionListingBase : ComponentBase
    {
        public class SubscriptionOrderDto
        {
            public int OrderId { get; set; }
            public string SubscriptionType { get; set; }
            public string UserName { get; set; }
            public string Email { get; set; }
            public string Mobile { get; set; }
            public string Whatsapp { get; set; }
            public string WebUrl { get; set; }
            public decimal Amount { get; set; }
            public string Status { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int WebLeads { get; set; }
            public int EmailLeads { get; set; }
            public int WhatsappLeads { get; set; }
            public int PhoneLeads { get; set; }
        }
        protected List<SubscriptionOrderDto> Listings { get; set; } = new();
        private SubscriptionOrderDto selectedOrder = new();
        protected HashSet<SubscriptionOrderDto> SelectedListings { get; set; } = new();
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
        private List<SubscriptionOrderDto> GetSampleData()
        {
            DateTime commonDate = DateTime.Parse("2025-04-12");
            return new List<SubscriptionOrderDto>
            {
                new SubscriptionOrderDto
                {
                    OrderId = 21435,
                    SubscriptionType = "12 Months Plus",
                    UserName = "Rashid",
                    Email = "Rashid.r@gmail.com",
                    Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Amount = 250,
            Status = "Active",
            StartDate = commonDate,
            EndDate = commonDate,
            WebLeads = 1,
            EmailLeads = 1,
            WhatsappLeads = 1,
            PhoneLeads = 2
        },
        new SubscriptionOrderDto
        {
            OrderId = 21435,
            SubscriptionType = "12 Months Super",
            UserName = "Rashid",
            Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Amount = 250,
            Status = "On Hold",
            StartDate = commonDate,
            EndDate = commonDate,
            WebLeads = 2,
            EmailLeads = 2,
            WhatsappLeads = 12,
            PhoneLeads = 2
        },
        new SubscriptionOrderDto
        {
            OrderId = 21342,
            SubscriptionType = "12 Months Super",
            UserName = "Rashid",
            Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Amount = 250,
            Status = "Active",
            StartDate = commonDate,
            EndDate = commonDate,
            WebLeads = 3,
            EmailLeads = 3,
            WhatsappLeads = 12,
            PhoneLeads = 1
        },
        new SubscriptionOrderDto
        {
            OrderId = 23415,
            SubscriptionType = "12 Months Super",
            UserName = "Rashid",
            Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Amount = 250,
            Status = "Cancelled",
            StartDate = commonDate,
            EndDate = commonDate,
            WebLeads = 4,
            EmailLeads = 4,
            WhatsappLeads = 2,
            PhoneLeads = 3
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
        protected void EditOrder(SubscriptionOrderDto order)
        {
            selectedOrder = new SubscriptionOrderDto
            {
                OrderId = order.OrderId,
                SubscriptionType = order.SubscriptionType,
                UserName = order.UserName,
                Email = order.Email,
                Mobile = order.Mobile,
                Whatsapp = order.Whatsapp,
                WebUrl = order.WebUrl,
                Amount = order.Amount,
                Status = order.Status,
                StartDate = order.StartDate,
                EndDate = order.EndDate,
                WebLeads = order.WebLeads,
                EmailLeads = order.EmailLeads,
                WhatsappLeads = order.WhatsappLeads,
                PhoneLeads = order.PhoneLeads
            };

            // You can open a dialog or set a flag to show a UI panel
            Console.WriteLine($"Editing Order ID: {order.OrderId}");
        }
        protected void CancelOrder(SubscriptionOrderDto order)
        {
            order.Status = "Cancelled";
            Console.WriteLine($"Cancelled Order ID: {order.OrderId}");
        }
    }
}
