using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Classified.DealsMenu
{
    public partial class DealsTableBase : QLComponentBase
    {
        [Inject] public IDealsService DealsService { get; set; }
        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] public ILogger<DealsTableBase> Logger { get; set; }
        [Parameter] public List<DealsItem> Listings { get; set; }
        [Parameter] public bool IsLoading { get; set; }
        [Parameter] public bool IsEmpty { get; set; }
        [Inject] public IDialogService DialogService { get; set; }

        [Parameter] public int TotalCount { get; set; }
        [Parameter] public EventCallback<int> OnPageChanged { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChanged { get; set; }
        [Parameter] public string SelectedTab { get; set; }
        [Parameter] public EventCallback<string> OnTabChanged { get; set; }
        [Parameter] public EventCallback<string> SelectedTabChanged { get; set; }

        protected bool isBulkActionLoading = false;
        protected long singleItemLoadingId = 0;
        protected long rejectionTargetItemId = 0;
        protected long removeTargetItemId = 0;
        protected bool isBulkRemove = false;

        protected HashSet<DealsItem> SelectedListings { get; set; } = [];
        protected int currentPage = 1;
        protected int pageSize = 12;

        protected string _activeTab;

        protected async Task HandleTabChanged(string newTab)
        {
            if (_activeTab != newTab)
            {
                _activeTab = newTab;
                await OnTabChanged.InvokeAsync(newTab);
                await SelectedTabChanged.InvokeAsync(newTab);
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
        }

        protected List<ToggleTabs.TabOption> tabOptions =
        [
          new() { Label = "Published", Value = "published" },
          new() { Label = "Unpublished", Value = "unPublished" }
        ];

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

        protected void OnEdit(long? adId)
        {
            NavManager.NavigateTo($"/manage/classified/deals/edit/ad/{adId}", true);
        }

        protected void OnPreview(DealsItem item)
        {
            Console.WriteLine($"Preview clicked: {item.Dealtitle}");
        }

        protected Task ApproveSelected() => PerformBulkAction(BulkActionEnum.Approve);

        protected Task RemoveSelected()
        {
            if (SelectedListings.Count == 0)
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

        protected Task Approve(DealsItem item) => RunSingleAction(item.AdId, BulkActionEnum.Approve);
        protected Task Publish(DealsItem item) => RunSingleAction(item.AdId, BulkActionEnum.Publish);
        protected Task Unpublish(DealsItem item) => RunSingleAction(item.AdId, BulkActionEnum.Unpublish);

        protected Task OnRemove(DealsItem item)
        {
            removeTargetItemId = item.AdId;
            isBulkRemove = false;
            OpenRemoveReasonDialog();
            return Task.CompletedTask;
        }

        private async Task RunSingleAction(long itemId, BulkActionEnum action)
        {
            singleItemLoadingId = itemId;
            await PerformBulkAction(action, "", [itemId]);
        }

        private async void OpenRemoveReasonDialog()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Remove Listing" },
                { "Description", "Please enter a reason before removing." },
                { "ButtonTitle", "Remove" },
                { "OnRejected", EventCallback.Factory.Create<string>(this, HandleNeedChangeWithReason) }
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            await DialogService.ShowAsync<RejectVerificationDialog>("", parameters, options);
        }
        protected async void OpenNeedChangeReasonDialog()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Request Need Change" },
                { "Description", "Please enter a reason before for requesting Need change." },
                { "ButtonTitle", "Need Change" },
                { "OnRejected", EventCallback.Factory.Create<string>(this, HandleNeedChangeWithReason) }
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
            else if (removeTargetItemId != 0)
            {
                singleItemLoadingId = removeTargetItemId;
                await PerformBulkAction(BulkActionEnum.Remove, reason, new List<long?> { removeTargetItemId });
                removeTargetItemId = 0;
            }

            isBulkRemove = false;
        }
        private async Task HandleNeedChangeWithReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return;

            await PerformBulkAction(BulkActionEnum.NeedChanges, reason);
        }

        protected Task RequestChanges(DealsListingModal item)
        {
            OpenRejectDialog(item.AdId ?? 0);
            return Task.CompletedTask;
        }

        private async void OpenRejectDialog(long adId)
        {
            rejectionTargetItemId = adId;
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
                BulkActionEnum.Approve => "Deals approved successfully.",
                BulkActionEnum.Publish => "Deals published successfully.",
                BulkActionEnum.Unpublish => "Deals unpublished successfully.",
                BulkActionEnum.UnPromote => "Deals un-promoted successfully.",
                BulkActionEnum.UnFeature => "Deals un-featured successfully.",
                BulkActionEnum.Remove => "Deals removed successfully.",
                BulkActionEnum.NeedChanges => "Request for changes sent successfully.",
                _ => "Action performed successfully."
            };
        }

        private async Task PerformBulkAction(BulkActionEnum action, string reason = "", List<long?>? adIds = null)
        {
            isBulkActionLoading = adIds == null;

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
                var response = await DealsService.BulkActionAsync(adIds, (int)action, reason);

                if (response?.IsSuccessStatusCode == true)
                {
                    SelectedListings.Clear();
                    Listings = Listings.Where(i => !adIds.Contains(i.AdId)).ToList();
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
                singleItemLoadingId = 0;
                StateHasChanged();
            }
        }
    }
}
