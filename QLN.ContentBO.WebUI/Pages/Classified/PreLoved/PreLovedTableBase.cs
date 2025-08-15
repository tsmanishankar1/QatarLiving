using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;
using QLN.ContentBO.WebUI.Interfaces;
using System.Text.Json;
using QLN.ContentBO.WebUI.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.PreLoved
{
    public partial class PreLovedTableBase : QLComponentBase
    {
        [Inject] public IPrelovedService PrelovedService { get; set; }
        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] public ILogger<PreLovedTableBase> Logger { get; set; }
        [Inject] public IDialogService DialogService { get; set; }

        [Parameter] public EventCallback<string> SelectedTabChanged { get; set; }
        [Parameter] public List<PrelovedP2PSubscriptionItem> Listings { get; set; }
        [Parameter] public bool IsLoading { get; set; }
        [Parameter] public bool IsEmpty { get; set; }
        [Parameter] public int TotalCount { get; set; }
        [Parameter] public EventCallback<int> OnPageChanged { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChanged { get; set; }
        [Parameter] public string SelectedTab { get; set; }
        [Parameter] public EventCallback<string> OnTabChanged { get; set; }

        protected bool isBulkActionLoading = false;
        protected int? singleItemLoadingId = null;
        protected int rejectionTargetItemId = 0;
        protected int removeTargetItemId = 0;
        protected bool isBulkRemove = false;

        protected HashSet<PrelovedP2PSubscriptionItem> SelectedListings { get; set; } = [];
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected string _activeTab;


        protected List<ToggleTabs.TabOption> tabOptions =
        [
            new() { Label = "Pending Approval", Value = "Pending Approval" },
            new() { Label = "Published", Value = "Published" },
            new() { Label = "Unpublished", Value = "Unpublished" },
            new() { Label = "Promoted", Value = "Promoted" },
            new() { Label = "Featured", Value = "Featured"  }
        ];

        protected async Task HandleTabChanged(string newTab)
        {
            if (_activeTab != newTab)
            {
                _activeTab = newTab;
                await OnTabChanged.InvokeAsync(newTab);
                await SelectedTabChanged.InvokeAsync(newTab); // Notify parent of change
            }
        }

        protected override Task OnParametersSetAsync()
        {
            if (!string.IsNullOrEmpty(SelectedTab))
            {
                _activeTab = SelectedTab;
            }
            else
            {
                _activeTab = "Pending Approval"; // Defaults to Pending Approval
            }
            return base.OnParametersSetAsync();
        }

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

        protected void OnEdit(PrelovedP2PSubscriptionItem item)
        {
            var name = "Rashid";
            NavManager.NavigateTo($"/manage/classified/deals/createform/{item.UserName}");
        }

        protected void OnPreview(P2pListingModal item)
        {
            Console.WriteLine($"Preview clicked: {item.AdTitle}");
        }

        protected Task ApproveSelected() => PerformBulkAction(BulkActionEnum.Approve);

        protected async Task RemoveSelected()
        {
            if (SelectedListings.Count == 0)
            {
                Snackbar.Add("Please select at least one listing to remove.", Severity.Warning);
            }

            isBulkRemove = true;
            await OpenRemoveReasonDialog();
        }

        protected Task UnpublishSelected() => PerformBulkAction(BulkActionEnum.Unpublish);
        protected Task PublishSelected() => PerformBulkAction(BulkActionEnum.Publish);
        protected Task UnpromoteSelected() => PerformBulkAction(BulkActionEnum.UnPromote);
        protected Task UnfeatureSelected() => PerformBulkAction(BulkActionEnum.UnFeature);

        protected Task Approve(PrelovedP2PSubscriptionItem item) => RunSingleAction(item.AdId, BulkActionEnum.Approve);
        protected Task Publish(PrelovedP2PSubscriptionItem item) => RunSingleAction(item.AdId, BulkActionEnum.Publish);
        protected Task Unpublish(PrelovedP2PSubscriptionItem item) => RunSingleAction(item.AdId, BulkActionEnum.Unpublish);

        protected async Task OnRemove(PrelovedP2PSubscriptionItem item)
        {
            removeTargetItemId = item.AdId;
            isBulkRemove = false;
            await OpenRemoveReasonDialog();
        }

        private async Task RunSingleAction(int itemId, BulkActionEnum action)
        {
            singleItemLoadingId = itemId;
            await PerformBulkAction(action, "", [itemId]);
        }

        private async Task OpenRemoveReasonDialog()
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

            if (rejectionTargetItemId == 0)
                return;

            singleItemLoadingId = rejectionTargetItemId;

            await PerformBulkAction(BulkActionEnum.NeedChanges, reason, [rejectionTargetItemId]);

            rejectionTargetItemId = 0;
        }

        private async Task HandleRemoveWithReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return;

            if (isBulkRemove)
            {
                await PerformBulkAction(BulkActionEnum.Remove, reason);
            }
            else if (removeTargetItemId == 0)
            {
                singleItemLoadingId = removeTargetItemId;
                await PerformBulkAction(BulkActionEnum.Remove, reason, [removeTargetItemId]);
                removeTargetItemId = 0;
            }

            isBulkRemove = false;
        }

        protected async Task RequestChanges(PrelovedP2PSubscriptionItem item)
        {
            await OpenRejectDialog(item.AdId);
        }

        private async Task OpenRejectDialog(int itemId)
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

        private async Task PerformBulkAction(BulkActionEnum action, string reason = "", List<long?> adIds = null)
        {
            isBulkActionLoading = adIds == null; // only bulk shows spinner

            adIds ??= [.. SelectedListings.Select(x => x.AdId)];

            if (adIds.Count == 0)
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
                var response = await PrelovedService.BulkActionAsync(adIds, (int)action);

                if (response?.IsSuccessStatusCode == true)
                {
                    SelectedListings.Clear();
                    Listings = [.. Listings.Where(i => !adIds.Contains(i.AdId))];
                    Snackbar.Add(GetSuccessMessage(action), Severity.Success);

                }
                else
                {
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
            }
        }
    }
}
