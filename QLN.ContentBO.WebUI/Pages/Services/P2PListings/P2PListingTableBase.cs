using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;

namespace QLN.ContentBO.WebUI.Pages.Services
{
    public partial class P2PListingTableBase : ComponentBase
    {
        [Inject] public IDialogService DialogService { get; set; }
        [Inject] public NavigationManager Navigation { get; set; }
        [Parameter] public EventCallback<int> OnPageChange { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }
        [Parameter]
        public EventCallback<int?> OnStatusChanged { get; set; }
        protected HashSet<ServiceAdSummaryDto> SelectedListings { get; set; } = new();
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected string selectedTab = "pendingApproval";
        protected int TotalCount => Listings.Count;
        [Parameter]
        public List<ServiceAdSummaryDto> Listings { get; set; } = new();
        protected List<ToggleTabs.TabOption> tabOptions = new()
        {
            new() { Label = "Pending Approval", Value = "pendingApproval" },
            new() { Label = "Published", Value = "published" },
            new() { Label = "Unpublished", Value = "unpublished" },
            new() { Label = "Promoted", Value = "promoted" },
            new() { Label = "Featured", Value = "featured" }
        };
        protected async Task OnTabChanged(string newTab)
        {
            selectedTab = newTab;

            int? status = newTab switch
            {
                "pendingApproval" => 2,
                "published" => 3,
                "unpublished" => 4,
                "Promoted" => 4,
                "featured" => 5,
                _ => null
            };
            await OnStatusChanged.InvokeAsync(status); 
        }
        protected Task ApproveSelected() => Task.Run(() => Console.WriteLine("Approved Selected"));
        protected Task UnpublishSelected() => Task.Run(() => Console.WriteLine("Unpublished Selected"));
        protected Task PublishSelected() => Task.Run(() => Console.WriteLine("Published Selected"));
        protected Task RemoveSelected() => Task.Run(() => Console.WriteLine("Removed Selected"));
        protected Task UnpromoteSelected() => Task.Run(() => Console.WriteLine("Unpromoted Selected"));
        protected Task UnfeatureSelected() => Task.Run(() => Console.WriteLine("Unfeatured Selected"));

        protected Task Approve(ServiceAdSummaryDto item) => Task.Run(() => Console.WriteLine($"Approved: {item.Id}"));
        protected Task Publish(ServiceAdSummaryDto item) => Task.Run(() => Console.WriteLine($"Published: {item.Id}"));
        protected Task Unpublish(ServiceAdSummaryDto item) => Task.Run(() => Console.WriteLine($"Unpublished: {item.Id}"));
        protected Task OnRemove(ServiceAdSummaryDto item) => Task.Run(() => Console.WriteLine($"Removed: {item.Id}"));
        private void HandleRejection(string reason)
        {
            Console.WriteLine("Rejection Reason: " + reason);
            // Send to API or handle in state
        }
        protected Task RequestChanges(ServiceAdSummaryDto item)
        {
            Console.WriteLine($"Requested changes for: {item.Id}");
            OpenRejectDialog();
            return Task.CompletedTask;
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
            // var dialog = DialogService.Show<RejectVerificationDialog>("", parameters, options);
        }




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
        public void OnEdit(ServiceAdSummaryDto item)
        {
            Navigation.NavigateTo("/manage/services/editform");
        }
        public void OnPreview(ServiceAdSummaryDto item)
        {
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
    }
}