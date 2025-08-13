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
        [Inject] public IClassifiedService ClassifiedService { get; set; }
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
        protected string? singleItemLoadingId = null;
        protected string rejectionTargetItemId = null;
        protected string removeTargetItemId = null;
        protected bool isBulkRemove = false;

        protected HashSet<PrelovedP2PSubscriptionItem> SelectedListings { get; set; } = new();
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected string _activeTab;


        protected List<ToggleTabs.TabOption> tabOptions =
        [
            new() { Label = "Pending Approval", Value = "Pending Approval" },
            new() { Label = "Published", Value = "Published" },
            new() { Label = "Unpublished", Value = "Unpublished" },
            new() { Label = "Promoted", Value = "Promoted" },
            new() { Label = "Promoted", Value = "Promoted"  }
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

        protected void OnEdit(SubscriptionListingModal item)
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

        protected Task Approve(P2pListingModal item) => RunSingleAction(item.Id, BulkActionEnum.Approve);
        protected Task Publish(P2pListingModal item) => RunSingleAction(item.Id, BulkActionEnum.Publish);
        protected Task Unpublish(P2pListingModal item) => RunSingleAction(item.Id, BulkActionEnum.Unpublish);

        protected async Task OnRemove(P2pListingModal item)
        {
            removeTargetItemId = item.Id;
            isBulkRemove = false;
            await OpenRemoveReasonDialog();
        }

        private async Task RunSingleAction(string itemId, BulkActionEnum action)
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
                await PerformBulkAction(BulkActionEnum.Remove, reason, [removeTargetItemId]);
                removeTargetItemId = null;
            }

            isBulkRemove = false;
        }

        protected async Task RequestChanges(P2pListingModal item)
        {
            await OpenRejectDialog(item.Id);
        }

        private async Task OpenRejectDialog(string itemId)
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

        private async Task PerformBulkAction(BulkActionEnum action, string reason = "", List<int>? adIds = null)
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
                var response = await ClassifiedService.PerformPrelovedBulkActionAsync(payload);

                if (response?.IsSuccessStatusCode == true)
                {
                    SelectedListings.Clear();
                    Listings = Listings.Where(i => !adIds.Contains(i.AdId)).ToList();
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
            }
        }
    }
}
