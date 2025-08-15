using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Enums;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.ViewListing
{
    public partial class ViewListingTableBase : QLComponentBase
    {
        [Inject] public IItemService ItemsService { get; set; }
        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] public IDialogService DialogService { get; set; }
        [Parameter] public bool IsLoading { get; set; }
        [Inject] public ILogger<ViewListingTableBase> Logger { get; set; }
        [Parameter] public List<ClassifiedItemViewListing> Items { get; set; } = [];
        [Parameter] public int TotalCount { get; set; }
        [Parameter]
        public EventCallback<string> OnTabChange { get; set; }
        [Parameter] public EventCallback<int> OnPageChanged { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChanged { get; set; }
        [Parameter] public EventCallback OnAddClicked { get; set; }
        protected HashSet<ClassifiedItemViewListing> SelectedListings { get; set; } = [];
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected bool isBulkActionLoading = false;
        protected long? singleItemLoadingId = null;
        protected long? rejectionTargetItemId = null;
        protected long? removeTargetItemId = null;
        protected bool isBulkRemove = false;

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
            new() { Label = "Need Changes", Value = "needChanges" },
            new() { Label = "All", Value = "all" },
            new() { Label = "Published", Value = "published" },
            new() { Label = "Unpublished", Value = "unpublished" },
            new() { Label = "P2P", Value = "p2p" },
            new() { Label = "Promoted", Value = "promoted" },
            new() { Label = "Featured", Value = "featured" },
            new() { Label = "Removed", Value = "removed" }
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
                "needChanges" => "Need Changes",
                "removed" => "Removed",
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

            var dialog = await DialogService.ShowAsync<ConfirmationDialog>("", parameters, options);
            var result = await dialog.Result;

        }
        private async void OpenRejectDialog(long? itemId)
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

        protected void OnEdit(ClassifiedItemViewListing item)
        {
            if (item?.Id is long id) 
            {
                NavManager.NavigateTo($"/manage/classified/items/edit/ad/{id}");
            }
            else
            {
                Console.WriteLine("Item ID is null — cannot navigate to edit page.");
            }
        }

        protected void OnPreview(ClassifiedItemViewListing item)
        {
            Console.WriteLine($"Preview clicked: {item.Title}");
        }

        protected Task ApproveSelected() => PerformBulkAction(AdBulkActionType.Approve);
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
        protected Task UnpublishSelected() => PerformBulkAction(AdBulkActionType.Unpublish);
        protected Task PublishSelected() => PerformBulkAction(AdBulkActionType.Publish);
        protected Task UnpromoteSelected() => PerformBulkAction(AdBulkActionType.UnPromote);
        protected Task UnfeatureSelected() => PerformBulkAction(AdBulkActionType.UnFeature);

        protected Task Approve(ClassifiedItemViewListing item) => RunSingleAction(item.Id, AdBulkActionType.Approve);
        protected Task Publish(ClassifiedItemViewListing item) => RunSingleAction(item.Id, AdBulkActionType.Publish);
        protected Task Unpublish(ClassifiedItemViewListing item) => RunSingleAction(item.Id, AdBulkActionType.Unpublish);
        protected Task OnRemove(ClassifiedItemViewListing item)
        {
            removeTargetItemId = item.Id;
            isBulkRemove = false;
            OpenRemoveReasonDialog();
            return Task.CompletedTask;
        }
        private async Task RunSingleAction(long? itemId, AdBulkActionType action)
        {
            singleItemLoadingId = itemId;
            await PerformBulkAction(action, "", [itemId]);
        }

        private async Task HandleRejection(string reason)
        {

            if (rejectionTargetItemId == null)
                return;

            singleItemLoadingId = rejectionTargetItemId;

            await PerformBulkAction(AdBulkActionType.NeedChanges, reason, [rejectionTargetItemId]);

            rejectionTargetItemId = null;
        }
        private async Task HandleRemoveWithReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return;

            if (isBulkRemove)
            {
                await PerformBulkAction(AdBulkActionType.Remove, reason);
            }
            else if (removeTargetItemId != null)
            {
                singleItemLoadingId = removeTargetItemId;
                await PerformBulkAction(AdBulkActionType.Remove, reason, new List<long?> { removeTargetItemId });
                removeTargetItemId = null;
            }

            isBulkRemove = false;
        }

        protected Task RequestChanges(ClassifiedItemViewListing item)
        {
            OpenRejectDialog(item.Id);
            return Task.CompletedTask;
        }

        private string GetSuccessMessage(AdBulkActionType action)
        {
            return action switch
            {
                AdBulkActionType.Approve => "Items approved successfully.",
                AdBulkActionType.Publish => "Items published successfully.",
                AdBulkActionType.Unpublish => "Items unpublished successfully.",
                AdBulkActionType.UnPromote => "Items un-promoted successfully.",
                AdBulkActionType.UnFeature => "Items un-featured successfully.",
                AdBulkActionType.Remove => "Items removed successfully.",
                AdBulkActionType.NeedChanges => "Request for changes sent successfully.",
                _ => "Action performed successfully."
            };
        }

        private async Task PerformBulkAction(AdBulkActionType action, string reason = "", List<long?> adIds = null)
        {
            isBulkActionLoading = adIds == null; // only bulk shows spinner

            adIds ??= [.. SelectedListings.Select(x => x.Id)];

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
                var response = await ItemsService.BulkItemsActionAsync(adIds, (int)action, reason);

                if (response?.IsSuccessStatusCode == true)
                {
                    SelectedListings.Clear();
                    // Remove from Items list directly
                    Items = [.. Items.Where(i => !adIds.Contains(i.Id))];
                    Snackbar.Add(GetSuccessMessage(action), Severity.Success);
                }
                else
                {
                    string errorMessage = "Something went wrong.";

                    if (response != null)
                    {
                        try
                        {
                            var errorJson = await response.Content.ReadAsStringAsync();

                            var errorObj = JsonSerializer.Deserialize<ProblemDetails>(errorJson, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (!string.IsNullOrWhiteSpace(errorObj?.Detail))
                            {
                                errorMessage = errorObj.Detail;
                            }
                        }
                        catch
                        {
                            errorMessage = $"Error {response.StatusCode}";
                        }
                    }

                    Snackbar.Add(errorMessage, Severity.Error);
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
