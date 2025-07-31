using Microsoft.AspNetCore.Components;
using System;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;
using QLN.Common.Infrastructure.Model;

namespace QLN.ContentBO.WebUI.Pages.Classified.PreLoved
{
    public partial class PreLovedTableBase : ComponentBase
    {
        [Parameter]
        public List<P2pListingModal> Listings { get; set; }
        [Parameter]
        public bool IsLoading { get; set; }
        [Parameter]
        public bool IsEmpty { get; set; }
        [Inject] public IDialogService DialogService { get; set; }

        [Parameter]
        public int TotalCount { get; set; }
        [Parameter]
        public EventCallback<int> OnPageChanged { get; set; }

        [Parameter]
        public EventCallback<int> OnPageSizeChanged { get; set; }

        // In PreLovedTableBase.cs
        [Parameter]
        public string SelectedTab { get; set; }  

        [Parameter]
        public EventCallback<string> OnTabChanged { get; set; }

        protected HashSet<P2pListingModal> SelectedListings { get; set; } = new();
        protected int currentPage = 1;
        protected int pageSize = 12;

        //protected string selectedTab = ((int)AdStatus.PendingApproval).ToString();
        protected string _activeTab;
        [Parameter]
        public EventCallback<string> selectedTabChanged { get; set; }

        protected async Task HandleTabChanged(string newTab)
        {
            if (_activeTab != newTab)
            {
                _activeTab = newTab;
                await OnTabChanged.InvokeAsync(newTab);
                await selectedTabChanged.InvokeAsync(newTab); // Notify parent of change
            }
        }
        protected override void OnParametersSet()
        {
            Console.WriteLine($"Parent SelectedTab: {SelectedTab}");
            Console.WriteLine($"Child received selectedTab: {SelectedTab}");
            Console.WriteLine($"Received selectedTab: {SelectedTab}");
            if (!string.IsNullOrEmpty(SelectedTab))
            {
                _activeTab = SelectedTab;
            }
            else
            {
                _activeTab = ((int)AdStatus.PendingApproval).ToString();
            }
            Console.WriteLine($"Active tab set to: {_activeTab}");
        }
        protected List<ToggleTabs.TabOption> tabOptions = new()
        {
    new() { Label = "Pending Approval", Value = ((int)AdStatus.PendingApproval).ToString() },
    new() { Label = "Published", Value = ((int)AdStatus.Published).ToString() },
    new() { Label = "Unpublished", Value = ((int)AdStatus.Unpublished).ToString() },
    new() { Label = "Promoted", Value = "promoted" },
    new() { Label = "Featured", Value = "featured"  }
};



        protected async Task HandlePageChange(int newPage)
        {
            currentPage = newPage;
            await OnPageChanged.InvokeAsync(newPage);
        }

        protected async Task HandlePageSizeChange(int newSize)
        {
            pageSize = newSize;
            currentPage = 1;
            await OnPageSizeChanged.InvokeAsync(newSize);
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


        protected void OnEdit(P2pListingModal item)
        {
            Console.WriteLine($"Edit clicked: {item.AdTitle}");
        }

        protected void OnPreview(P2pListingModal item)
        {
            Console.WriteLine($"Preview clicked: {item.AdTitle}");
        }

        protected Task ApproveSelected() => Task.Run(() => Console.WriteLine("Approved Selected"));
        protected Task UnpublishSelected() => Task.Run(() => Console.WriteLine("Unpublished Selected"));
        protected Task PublishSelected() => Task.Run(() => Console.WriteLine("Published Selected"));
        protected Task RemoveSelected() => Task.Run(() => Console.WriteLine("Removed Selected"));
        protected Task UnpromoteSelected() => Task.Run(() => Console.WriteLine("Unpromoted Selected"));
        protected Task UnfeatureSelected() => Task.Run(() => Console.WriteLine("Unfeatured Selected"));

        protected Task Approve(P2pListingModal item) => Task.Run(() => Console.WriteLine($"Approved: {item.Id}"));
        protected Task Publish(P2pListingModal item) => Task.Run(() => Console.WriteLine($"Published: {item.Id}"));
        protected Task Unpublish(P2pListingModal item) => Task.Run(() => Console.WriteLine($"Unpublished: {item.Id}"));
        protected Task OnRemove(P2pListingModal item) => Task.Run(() => Console.WriteLine($"Removed: {item.Id}"));
        private void HandleRejection(string reason)
        {
            Console.WriteLine("Rejection Reason: " + reason);
        }
        protected Task RequestChanges(P2pListingModal item)
        {
            Console.WriteLine($"Requested changes for: {item.Id}");
            OpenRejectDialog();
            return Task.CompletedTask;
        }
    }
}
