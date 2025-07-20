using Microsoft.AspNetCore.Components;
using System;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;
using QLN.ContentBO.WebUI.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.DealsMenu
{
    public partial class DealsTableBase : QLComponentBase
    {
        protected List<ListingItem> Listings { get; set; } = new();
        protected HashSet<ListingItem> SelectedListings { get; set; } = new();
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
        protected string selectedTab = "pendingApproval";

       protected List<ToggleTabs.TabOption> tabOptions = new()
        {
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
        private List<ListingItem> GetSampleData()
        {
            string imageUrl = "qln-images/preloved_mocked.png";
            return new List<ListingItem>
            {
                new ListingItem { AdId = 21660, UserId = 21660, AdTitle = "QPO Presents", InternalUserId = 23, UserName = "Rashid", Category = "Electronics", SubCategory = "Phone", Section = "Apple", CreationDate = DateTime.Parse("2025-04-12"), PublishedDate = DateTime.Parse("2025-04-12"), ExpiryDate = DateTime.Parse("2025-04-12"), ImageUrl = imageUrl },
                new ListingItem { AdId = 21435, UserId = 21435, AdTitle = "LEGO® Show", InternalUserId = 23, UserName = "Rashid", Category = "Electronics", SubCategory = "Phone", Section = "Apple", CreationDate = DateTime.Parse("2025-04-12"), PublishedDate = DateTime.Parse("2025-04-12"), ExpiryDate = DateTime.Parse("2025-04-12"), ImageUrl = imageUrl },
                new ListingItem { AdId = 21342, UserId = 21342, AdTitle = "Feast and Be...", InternalUserId = 23, UserName = "Rashid", Category = "Electronics", SubCategory = "Phone", Section = "Apple", CreationDate = DateTime.Parse("2025-04-12"), PublishedDate = DateTime.Parse("2025-04-12"), ExpiryDate = DateTime.Parse("2025-04-12"), ImageUrl = imageUrl },
                new ListingItem { AdId = 23415, UserId = 23415, AdTitle = "Candlelight: T...", InternalUserId = 23, UserName = "Rashid", Category = "Electronics", SubCategory = "Phone", Section = "Apple", CreationDate = DateTime.Parse("2025-04-12"), PublishedDate = DateTime.Parse("2025-04-12"), ExpiryDate = DateTime.Parse("2025-04-12"), ImageUrl = imageUrl }
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
    }
}
