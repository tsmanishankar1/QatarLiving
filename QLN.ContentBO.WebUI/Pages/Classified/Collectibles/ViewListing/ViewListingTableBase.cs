using Microsoft.AspNetCore.Components;
using System;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using System.Collections.Generic;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;

namespace QLN.ContentBO.WebUI.Pages.Classified.Collectibles.ViewListing
{
    public partial class ViewListingTableBase : ComponentBase
    {
        [Parameter] public bool IsLoading { get; set; }
        [Parameter] public List<ClassifiedItemViewListing> Items { get; set; } = new();
        [Parameter] public int TotalCount { get; set; }
        [Parameter]
        public EventCallback<string> OnTabChange { get; set; }
        [Parameter] public EventCallback<int> OnPageChanged { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChanged { get; set; }
        [Inject] public NavigationManager Navigation { get; set; }
        protected HashSet<ClassifiedItemViewListing> SelectedListings { get; set; } = new();
        [Inject] public IDialogService DialogService { get; set; }
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


        protected string selectedTab = "pendingApproval";

        protected List<ToggleTabs.TabOption> tabOptions = new()
        {
            new() { Label = "Pending Approval", Value = "pendingApproval" },
            new() { Label = "All", Value = "all" },
            new() { Label = "Published", Value = "published" },
            new() { Label = "Unpublished", Value = "unpublished" },
            new() { Label = "P2P", Value = "p2p" },
            new() { Label = "Promoted", Value = "promoted" },
            new() { Label = "Featured", Value = "featured" }
        };
        protected string GetTabTitle()
        {
            return selectedTab switch
            {
                "all" => "All",
                "pendingApproval" => "Pending Approval",
                "published" => "Published",
                "unpublished" => "Unpublished",
                "p2p" => "P2P",
                "promoted" => "Promoted",
                "featured" => "Featured",
                _ => "Classified"
            };
        }
         protected async Task OnTabChanged(string newTab)
        {
            selectedTab = newTab;
            SelectedListings.Clear();
            await OnTabChange.InvokeAsync(newTab);
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
          protected void OnEdit(ClassifiedItemViewListing item)
        {
            var targetUrl = $"/manage/classified/collectibles/edit/ad/{item.Id}";
            Navigation.NavigateTo(targetUrl);

        }

        protected void OnPreview(ClassifiedItemViewListing item)
        {
            Console.WriteLine($"Preview clicked: {item.Title}");
        }

        protected Task ApproveSelected() => Task.Run(() => Console.WriteLine("Approved Selected"));
        protected Task UnpublishSelected() => Task.Run(() => Console.WriteLine("Unpublished Selected"));
        protected Task PublishSelected() => Task.Run(() => Console.WriteLine("Published Selected"));
        protected Task RemoveSelected() => Task.Run(() => Console.WriteLine("Removed Selected"));
        protected Task UnpromoteSelected() => Task.Run(() => Console.WriteLine("Unpromoted Selected"));
        protected Task UnfeatureSelected() => Task.Run(() => Console.WriteLine("Unfeatured Selected"));

        protected Task Approve(ClassifiedItemViewListing item) => Task.Run(() => Console.WriteLine($"Approved: {item.Id}"));
        protected Task Publish(ClassifiedItemViewListing item) => Task.Run(() => Console.WriteLine($"Published: {item.Id}"));
        protected Task Unpublish(ClassifiedItemViewListing item) => Task.Run(() => Console.WriteLine($"Unpublished: {item.Id}"));
        protected Task OnRemove(ClassifiedItemViewListing item) => Task.Run(() => Console.WriteLine($"Removed: {item.Id}"));
        private void HandleRejection(string reason)
        {
            Console.WriteLine("Rejection Reason: " + reason);
            // Send to API or handle in state
        }
        protected Task RequestChanges(ClassifiedItemViewListing item)
        {
            Console.WriteLine($"Requested changes for: {item.Id}");
            OpenRejectDialog();
            return Task.CompletedTask;
        }
     
    }
}
