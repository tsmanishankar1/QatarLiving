using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Interfaces;

using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;

namespace QLN.ContentBO.WebUI.Pages.Services
{
    public partial class ViewSubscriptionListingTableBase : ComponentBase
    {
        [Inject] public IDialogService DialogService { get; set; }
        [Inject] public NavigationManager Navigation { get; set; }
        protected HashSet<ServiceAdPaymentSummaryDto> SelectedListings { get; set; } = new();
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] public IServiceBOService _serviceBOService { get; set; }
        [Parameter] public EventCallback<int> OnPageChange { get; set; }
        [Parameter] public EventCallback OnListReload { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }
         public string? rejectReason { get; set; } = string.Empty;
        public Guid selectedAdId { get; set; }
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected int TotalCount => Listings.Count;
        [Parameter]
        public List<ServiceAdPaymentSummaryDto> Listings { get; set; } = new();
        protected async void HandlePageChange(int newPage)
        {
            currentPage = newPage;
            await OnPageChange.InvokeAsync(currentPage);
        }

        protected async void HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            currentPage = 1;
            await OnPageSizeChange.InvokeAsync(pageSize);
        }
        public void OnEdit(ServiceAdPaymentSummaryDto item)
        {
            Navigation.NavigateTo($"/manage/services/editform/{item.AddId}");
        }
        public void OnPreview(ServiceAdPaymentSummaryDto item)
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
        protected void OpenRejectDialog(Guid guid)
        {
            selectedAdId = guid;
            var parameters = new DialogParameters
            {
                { "Title", "Remove Subscription" },
                { "Description", "Please enter a reason before removing" },
                { "ButtonTitle", "Remove" },
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
        private async Task HandleRejection(string reason)
        {
            rejectReason = string.IsNullOrWhiteSpace(reason) ? null : reason;
            await RemoveSubscription();
        }
        protected async Task RemoveSubscription()
        {
            var statusRequest = new BulkModerationRequest
            {
                AdIds = new List<Guid> { selectedAdId },
                Action = BulkModerationAction.Remove,
                Reason = rejectReason,
            };
            var response = await _serviceBOService.UpdateServiceStatus(statusRequest);
            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add("Removed Subscription Successfully", Severity.Success);
                await OnListReload.InvokeAsync();
                StateHasChanged();
            }
            else
            {
                Snackbar.Add("Failed to update ad status", Severity.Error);
            }
        }
    }
}