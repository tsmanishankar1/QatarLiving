using Microsoft.AspNetCore.Components;
using System;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;

namespace QLN.ContentBO.WebUI.Pages.Classified.DealsMenu
{
    public partial class DealsSubscriptionTableBase : ComponentBase
    {
        [Inject] public IDialogService DialogService { get; set; }
        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;
        protected override void OnInitialized()
        {
            Listings = GetSampleData();
        }
        protected List<SubscriptionListing> Listings { get; set; } = new();
        protected HashSet<SubscriptionListing> SelectedListings { get; set; } = new();
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
            currentPage = 1;
            StateHasChanged();
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
        private List<SubscriptionListing> GetSampleData()
        {
            return new List<SubscriptionListing>
    {
        new SubscriptionListing {
            AdId = 21435, UserId = 21435, AdTitle = "12 Months Plus",
            UserName = "Rashid", SubscriptionType = "12 Months Plus",
            CreationDate = DateTime.Parse("2025-04-12 00:00"), PublishedDate = DateTime.Parse("2025-04-12 00:00"),
            ExpiryDate = DateTime.Parse("2025-04-12 00:00"), Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537", Whatsapp = "+974 5030537", Amount = 250, Status = "Active"
        },
        new SubscriptionListing {
            AdId = 21435, UserId = 21435, AdTitle = "12 Months Super",
            UserName = "Rashid",SubscriptionType = "12 Months Plus",
            CreationDate = DateTime.Parse("2025-04-12 00:00"), PublishedDate = DateTime.Parse("2025-04-12 00:00"),
            ExpiryDate = DateTime.Parse("2025-04-12 00:00"), Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537", Whatsapp = "+974 5030537", Amount = 250, Status = "On Hold"
        },
        new SubscriptionListing {
            AdId = 21342, UserId = 21342, AdTitle = "12 Months Super", 
            UserName = "Rashid", SubscriptionType = "12 Months Plus",
            CreationDate = DateTime.Parse("2025-04-12 00:00"), PublishedDate = DateTime.Parse("2025-04-12 00:00"),
            ExpiryDate = DateTime.Parse("2025-04-12 00:00"), Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537", Whatsapp = "+974 5030537", Amount = 250, Status = "Active"
        },
        new SubscriptionListing {
            AdId = 23415, UserId = 23415, AdTitle = "12 Months Super", 
            UserName = "Rashid", SubscriptionType = "12 Months Plus",
            CreationDate = DateTime.Parse("2025-04-12 00:00"), PublishedDate = DateTime.Parse("2025-04-12 00:00"),
            ExpiryDate = DateTime.Parse("2025-04-12 00:00"), Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537", Whatsapp = "+974 5030537", Amount = 250, Status = "Cancelled"
        }
    };
        }

        protected void OnEdit(SubscriptionListing item)
        {
            var name = "Rashid";
            NavigationManager.NavigateTo($"/manage/classified/deals/createform/{name}");
        }

        protected void OnPreview(SubscriptionListing item)
        {
            Console.WriteLine($"Preview clicked: {item.AdTitle}");
        }

        protected Task ApproveSelected() => Task.Run(() => Console.WriteLine("Approved Selected"));
        protected Task UnpublishSelected() => Task.Run(() => Console.WriteLine("Unpublished Selected"));
        protected Task PublishSelected() => Task.Run(() => Console.WriteLine("Published Selected"));
        protected Task RemoveSelected() => Task.Run(() => Console.WriteLine("Removed Selected"));
        protected Task UnpromoteSelected() => Task.Run(() => Console.WriteLine("Unpromoted Selected"));
        protected Task UnfeatureSelected() => Task.Run(() => Console.WriteLine("Unfeatured Selected"));

        protected Task Approve(SubscriptionListing item) => Task.Run(() => Console.WriteLine($"Approved: {item.AdId}"));
        protected Task Publish(SubscriptionListing item) => Task.Run(() => Console.WriteLine($"Published: {item.AdId}"));
        protected Task Unpublish(SubscriptionListing item) => Task.Run(() => Console.WriteLine($"Unpublished: {item.AdId}"));
        protected Task OnRemove(SubscriptionListing item) => Task.Run(() => Console.WriteLine($"Removed: {item.AdId}"));
        private void HandleRejection(string reason)
        {
            Console.WriteLine("Rejection Reason: " + reason);
            // Send to API or handle in state
        }
        protected Task RequestChanges(SubscriptionListing item)
        {
            Console.WriteLine($"Requested changes for: {item.AdId}");
            OpenRejectDialog();
            return Task.CompletedTask;
        }
    }
}
