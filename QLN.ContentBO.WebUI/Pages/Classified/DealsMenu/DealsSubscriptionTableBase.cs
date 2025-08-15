using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;
using QLN.ContentBO.WebUI.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.DealsMenu
{
    public partial class DealsSubscriptionTableBase : QLComponentBase
    {
        [Inject] public IDialogService DialogService { get; set; }
        [Parameter] public List<DealsSubscriptionItem> Listings { get; set; } = [];
        [Parameter] public bool IsLoading { get; set; }
        [Parameter] public bool IsEmpty { get; set; }
        [Parameter] public int TotalCount { get; set; }
        [Parameter] public EventCallback<int> OnPageChanged { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChanged { get; set; }

        protected HashSet<DealsSubscriptionItem> SelectedListings { get; set; } = [];
        protected int currentPage = 1;
        protected int pageSize = 12;

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

        protected string selectedTab = "pendingApproval";

        protected List<ToggleTabs.TabOption> tabOptions =
        [
            new() { Label = "Pending Approval", Value = "pendingApproval" },
            new() { Label = "Published", Value = "published" },
            new() { Label = "Unpublished", Value = "unpublished" },
            new() { Label = "P2P", Value = "p2p" },
            new() { Label = "Promoted", Value = "promoted" },
            new() { Label = "Featured", Value = "featured" }
        ];

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

            var dialog = await DialogService.ShowAsync<ConfirmationDialog>("", parameters, options);
            var result = await dialog.Result;
        }

        private async void OpenRejectDialog()
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
            var dialog = await DialogService.ShowAsync<RejectVerificationDialog>("", parameters, options);
        }

        protected void OnEdit(DealsSubscriptionItem item)
        {
            NavManager.NavigateTo($"/manage/classified/deals/createform/{item.AdId}", true);
        }

        protected void OnPreview(DealsSubscriptionItem item)
        {
            Console.WriteLine($"Preview clicked: {item.AdId}");
        }

        protected Task ApproveSelected() => Task.Run(() => Console.WriteLine("Approved Selected"));
        protected Task UnpublishSelected() => Task.Run(() => Console.WriteLine("Unpublished Selected"));
        protected Task PublishSelected() => Task.Run(() => Console.WriteLine("Published Selected"));
        protected Task RemoveSelected() => Task.Run(() => Console.WriteLine("Removed Selected"));
        protected Task UnpromoteSelected() => Task.Run(() => Console.WriteLine("Unpromoted Selected"));
        protected Task UnfeatureSelected() => Task.Run(() => Console.WriteLine("Unfeatured Selected"));

        protected Task Approve(DealsSubscriptionItem item) => Task.Run(() => Console.WriteLine($"Approved: {item.AdId}"));
        protected Task Publish(DealsSubscriptionItem item) => Task.Run(() => Console.WriteLine($"Published: {item.AdId}"));
        protected Task Unpublish(DealsSubscriptionItem item) => Task.Run(() => Console.WriteLine($"Unpublished: {item.AdId}"));
        protected Task OnRemove(DealsSubscriptionItem item) => Task.Run(() => Console.WriteLine($"Removed: {item.AdId}"));

        private void HandleRejection(string reason)
        {
            Console.WriteLine("Rejection Reason: " + reason);
        }

        protected Task RequestChanges(DealsSubscriptionItem item)
        {
            Console.WriteLine($"Requested changes for: {item.AdId}");
            OpenRejectDialog();
            return Task.CompletedTask;
        }
    }
}
