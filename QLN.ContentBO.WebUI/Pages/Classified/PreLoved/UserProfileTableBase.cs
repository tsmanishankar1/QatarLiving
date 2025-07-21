using Microsoft.AspNetCore.Components;
using System;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;

namespace QLN.ContentBO.WebUI.Pages.Classified.PreLoved
{
    public partial class UserProfileTableBase : ComponentBase
    {
        protected List<UserModal> Listings { get; set; } = new();
        protected HashSet<UserModal> SelectedListings { get; set; } = new();
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
            currentPage = 1; 
            StateHasChanged();
        }


        protected override void OnInitialized()
        {
            Listings = GetSampleData();
        }
        protected string selectedTab = "verificationRequest";

       protected List<ToggleTabs.TabOption> tabOptions = new()
        {
            new() { Label = "Verification Request", Value = "verificationRequest" },
            new() { Label = "Rejected", Value = "rejected" },
            new() { Label = "Approved", Value = "approved" },
          
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
        private List<UserModal> GetSampleData()
        {
            return new List<UserModal>
            {
                new UserModal { UserId = 21660,  UserName = "Rashid", CreationDate = DateTime.Parse("2025-04-12"), PublishedDate = DateTime.Parse("2025-04-12"), ExpiryDate = DateTime.Parse("2025-04-12")},
                new UserModal { UserId = 21435,  UserName = "Walid",  CreationDate = DateTime.Parse("2025-04-12"), PublishedDate = DateTime.Parse("2025-04-12"), ExpiryDate = DateTime.Parse("2025-04-12")},
                new UserModal { UserId = 21342,  UserName = "Jamir",  CreationDate = DateTime.Parse("2025-04-12"), PublishedDate = DateTime.Parse("2025-04-12"), ExpiryDate = DateTime.Parse("2025-04-12")},
                new UserModal { UserId = 23415,  UserName = "Mohamed",CreationDate = DateTime.Parse("2025-04-12"), PublishedDate = DateTime.Parse("2025-04-12"), ExpiryDate = DateTime.Parse("2025-04-12")}
            };
        }

        protected void OnEdit(UserModal item)
        {
            Console.WriteLine($"Edit clicked: {item.UserId}");
        }

        protected void OnPreview(UserModal item)
        {
            Console.WriteLine($"Preview clicked: {item.UserId}");
        }

        protected Task ApproveSelected() => Task.Run(() => Console.WriteLine("Approved Selected"));
        protected Task UnpublishSelected() => Task.Run(() => Console.WriteLine("Unpublished Selected"));
        protected Task PublishSelected() => Task.Run(() => Console.WriteLine("Published Selected"));
        protected Task RemoveSelected() => Task.Run(() => Console.WriteLine("Removed Selected"));
        protected Task UnpromoteSelected() => Task.Run(() => Console.WriteLine("Unpromoted Selected"));
        protected Task UnfeatureSelected() => Task.Run(() => Console.WriteLine("Unfeatured Selected"));

        protected Task Approve(UserModal item) => Task.Run(() => Console.WriteLine($"Approved: {item.UserId}"));
        protected Task Publish(UserModal item) => Task.Run(() => Console.WriteLine($"Published: {item.UserId}"));
        protected Task Unpublish(UserModal item) => Task.Run(() => Console.WriteLine($"Unpublished: {item.UserId}"));
        protected Task OnRemove(UserModal item) => Task.Run(() => Console.WriteLine($"Removed: {item.UserId}"));
        private void HandleRejection(string reason)
        {
            Console.WriteLine("Rejection Reason: " + reason);
            // Send to API or handle in state
        }
        protected Task RequestChanges(UserModal item)
        {
            Console.WriteLine($"Requested changes for: {item.UserId}");
            OpenRejectDialog();
            return Task.CompletedTask;
        }
    }
}
