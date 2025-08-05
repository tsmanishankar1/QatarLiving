using Microsoft.AspNetCore.Components;
using System;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Pages.Classified.PreLoved;
using System.Text.Json;
using QLN.ContentBO.WebUI.Interfaces;
using static MudBlazor.Colors;

namespace QLN.ContentBO.WebUI.Pages.Classified.DealsMenu
{
    public partial class DealsTableBase : QLComponentBase
    {
        [Parameter]
        public List<DealsListingModal> Listings { get; set; }
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

        [Inject]
        public IClassifiedService ClassifiedService { get; set; }
        [Inject] public ISnackbar Snackbar { get; set; }
        protected bool isBulkActionLoading = false;
        protected string singleItemLoadingId = null;
        protected string rejectionTargetItemId = null;
        protected string removeTargetItemId = null;
        protected bool isBulkRemove = false;
        [Inject] public ILogger<DealsTableBase> Logger { get; set; }

        protected HashSet<DealsListingModal> SelectedListings { get; set; } = new();
        protected int currentPage = 1;
        protected int pageSize = 12;

        protected string _activeTab;
        [Parameter]
        public EventCallback<string> selectedTabChanged { get; set; }

        protected async Task HandleTabChanged(string newTab)
        {
            if (_activeTab != newTab)
            {
                _activeTab = newTab;
                await OnTabChanged.InvokeAsync(newTab);
                await selectedTabChanged.InvokeAsync(newTab); 
            }
        }
        protected override void OnParametersSet()
        {
           
            if (!string.IsNullOrEmpty(SelectedTab))
            {
                _activeTab = SelectedTab;
            }
            else
            {
                _activeTab = ((int)AdStatusEnum.PendingApproval).ToString();
            }
            Console.WriteLine($"Active tab set to: {_activeTab}");
        }
        protected List<ToggleTabs.TabOption> tabOptions = new()
        {
          new() { Label = "Published", Value = ((int)AdStatusEnum.Published).ToString() },
          new() { Label = "Unpublished", Value = ((int)AdStatusEnum.Unpublished).ToString() },
           new() { Label = "P2p", Value = ((int)AdStatusEnum.P2p).ToString() },
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

            var dialog = await DialogService.ShowAsync<ConfirmationDialog>("", parameters, options);
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

        protected void OnEdit(string adId)
        {
            NavManager.NavigateTo($"/manage/classified/deals/edit/ad/{adId}");
        }
        protected void OnPreview(DealsListingModal item)
        {
            Console.WriteLine($"Preview clicked: {item.DealTitle}");
        }

        protected Task ApproveSelected() => PerformBulkAction(BulkActionEnum.Approve);
        protected Task RemoveSelected()
        {
            if (!SelectedListings.Any())
            {
                Snackbar.Add("Please select at least one listing to remove.", Severity.Warning);
                return Task.CompletedTask;
            }

            isBulkRemove = true;
            OpenRemoveReasonDialog();
            return Task.CompletedTask;
        }
        protected Task UnpublishSelected() => PerformBulkAction(BulkActionEnum.Unpublish);

        protected Task PublishSelected() => PerformBulkAction(BulkActionEnum.Publish);
        protected Task UnpromoteSelected() => PerformBulkAction(BulkActionEnum.UnPromote);
        protected Task UnfeatureSelected() => PerformBulkAction(BulkActionEnum.UnFeature);

        protected Task Approve(DealsListingModal item) => RunSingleAction(item.Id, BulkActionEnum.Approve);
        protected Task Publish(DealsListingModal item) => RunSingleAction(item.Id, BulkActionEnum.Publish);
        protected Task Unpublish(DealsListingModal item) => RunSingleAction(item.Id, BulkActionEnum.Unpublish);
        protected Task OnRemove(DealsListingModal item)
        {
            removeTargetItemId = item.Id;
            isBulkRemove = false;
            OpenRemoveReasonDialog();
            return Task.CompletedTask;
        }
        private async Task RunSingleAction(string itemId, BulkActionEnum action)
        {
            singleItemLoadingId = itemId;
            await PerformBulkAction(action, "", new List<string> { itemId });
        }
        private async void OpenRemoveReasonDialog()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Remove Listing" },
                { "Description", "Please enter a reason before removing." },
                { "ButtonTitle", "Remove" },
                { "OnRejected", EventCallback.Factory.Create<string>(this, HandleRemoveWithReason) }
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            await DialogService.ShowAsync<RejectVerificationDialog>("", parameters, options);
        }
        private async Task HandleRejection(string reason)
        {
            Console.WriteLine($"Rejection Reason: {reason}");

            if (string.IsNullOrWhiteSpace(rejectionTargetItemId))
                return;

            singleItemLoadingId = rejectionTargetItemId;

            await PerformBulkAction(BulkActionEnum.NeedChanges, reason, new List<string> { rejectionTargetItemId });

            rejectionTargetItemId = null;
        }
        private async Task HandleRemoveWithReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return;

            if (isBulkRemove)
            {
                await PerformBulkAction(BulkActionEnum.Remove, reason);
            }
            else if (!string.IsNullOrWhiteSpace(removeTargetItemId))
            {
                singleItemLoadingId = removeTargetItemId;
                await PerformBulkAction(BulkActionEnum.Remove, reason, new List<string> { removeTargetItemId });
                removeTargetItemId = null;
            }

            isBulkRemove = false;
        }

        protected Task RequestChanges(DealsListingModal item)
        {
            OpenRejectDialog(item.Id);
            return Task.CompletedTask;
        }
        private async void OpenRejectDialog(string itemId)
        {
            rejectionTargetItemId = itemId;
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
        private string GetSuccessMessage(BulkActionEnum action)
        {
            return action switch
            {
                BulkActionEnum.Approve => "Items approved successfully.",
                BulkActionEnum.Publish => "Items published successfully.",
                BulkActionEnum.Unpublish => "Items unpublished successfully.",
                BulkActionEnum.UnPromote => "Items un-promoted successfully.",
                BulkActionEnum.UnFeature => "Items un-featured successfully.",
                BulkActionEnum.Remove => "Items removed successfully.",
                BulkActionEnum.NeedChanges => "Request for changes sent successfully.",
                _ => "Action performed successfully."
            };
        }
        private async Task PerformBulkAction(BulkActionEnum action, string reason = "", List<string> adIds = null)
        {
            isBulkActionLoading = adIds == null; 

            adIds ??= SelectedListings.Select(x => x.Id).ToList();

            if (!adIds.Any())
                return;

            var payload = new Dictionary<string, object>
            {
                ["adIds"] = adIds,
                ["action"] = (int)action,
                ["reason"] = reason
            };

            try
            {
                var payloadJson = JsonSerializer.Serialize(payload);
                Logger.LogInformation("Performing bulk action: {Payload}", payloadJson);
                var response = await ClassifiedService.PerformPrelovedBulkActionAsync(payload);

                if (response?.IsSuccessStatusCode == true)
                {
                    SelectedListings.Clear();
                    Listings = Listings.Where(i => !adIds.Contains(i.Id)).ToList();
                    Snackbar.Add(GetSuccessMessage(action), Severity.Success);

                }
                else
                {
                    // Logger.LogWarning("Bulk action failed. StatusCode: {StatusCode}, Payload: {@Payload}",
                    //     response?.StatusCode, payload);
                    Snackbar.Add("Something went wrong while performing the action.", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception occurred while performing bulk action with payload: {@Payload}", payload);
                Snackbar.Add("Unexpected error occurred during the action.", Severity.Error);
            }
            finally
            {
                isBulkActionLoading = false;
                singleItemLoadingId = null;
                StateHasChanged();
            }
        }
    }


}
