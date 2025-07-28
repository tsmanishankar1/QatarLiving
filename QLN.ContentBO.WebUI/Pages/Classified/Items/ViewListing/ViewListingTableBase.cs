using Microsoft.AspNetCore.Components;
using System;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using System.Collections.Generic;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;
using QLN.ContentBO.WebUI.Interfaces;
using MudBlazor;
using System.Linq;


namespace QLN.ContentBO.WebUI.Pages.Classified.Items.ViewListing
{
    public partial class ViewListingTableBase : ComponentBase
    {
        [Inject]
        public IClassifiedService ClassifiedService { get; set; }
        [Inject] public ISnackbar Snackbar { get; set; }
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
        public enum BulkActionType
        {
            Approve = 1,
            Publish = 2,
            Unpublish = 3,
            UnPromote = 5,
            UnFeature = 6,
            Remove = 7,
            NeedChanges = 8
        }


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
            var targetUrl = $"/manage/classified/items/edit/ad/{item.Id}";
            Navigation.NavigateTo(targetUrl);

        }

        protected void OnPreview(ClassifiedItemViewListing item)
        {
            Console.WriteLine($"Preview clicked: {item.Title}");
        }

        protected Task ApproveSelected() => PerformBulkAction(BulkActionType.Approve);
        protected Task RemoveSelected() => PerformBulkAction(BulkActionType.Remove);
        protected Task UnpublishSelected() => PerformBulkAction(BulkActionType.Unpublish);
        protected Task PublishSelected() => PerformBulkAction(BulkActionType.Publish);
        protected Task UnpromoteSelected() => PerformBulkAction(BulkActionType.UnPromote);
        protected Task UnfeatureSelected() => PerformBulkAction(BulkActionType.UnFeature);


        protected Task Approve(ClassifiedItemViewListing item) => Task.Run(() => Console.WriteLine($"Approved: {item.Id}"));
        protected Task Publish(ClassifiedItemViewListing item) => Task.Run(() => Console.WriteLine($"Published: {item.Id}"));
        protected Task Unpublish(ClassifiedItemViewListing item) => Task.Run(() => Console.WriteLine($"Unpublished: {item.Id}"));
        protected Task OnRemove(ClassifiedItemViewListing item) => Task.Run(() => Console.WriteLine($"Removed: {item.Id}"));
        private async Task HandleRejection(string reason)
        {
            Console.WriteLine("Rejection Reason: " + reason);
            
            await PerformBulkAction(BulkActionType.NeedChanges, reason);
        }

        protected Task RequestChanges(ClassifiedItemViewListing item)
        {
            OpenRejectDialog();
            return Task.CompletedTask;
        }
     
       private async Task PerformBulkAction(BulkActionType action, string reason = "")
        {
            var adIds = SelectedListings.Select(x => x.Id).ToList();

            if (!adIds.Any())
                return;

            var payload = new Dictionary<string, object>
            {
                ["adIds"] = adIds,
                ["action"] = (int)action,
                ["reason"] = reason
            };

            var response = await ClassifiedService.PerformBulkActionAsync(payload);

            if (response?.IsSuccessStatusCode == true)
            {
                SelectedListings.Clear();
                Snackbar.Add("Action performed successfully.", Severity.Success);
                await OnTabChange.InvokeAsync(selectedTab); // Refresh
            }
            else
            {
                Snackbar.Add("Something went wrong while performing the action.", Severity.Error);
            }
        }
    }
}
