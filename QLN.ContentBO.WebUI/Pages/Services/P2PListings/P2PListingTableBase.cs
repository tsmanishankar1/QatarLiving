using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Pages.Services.PreviewServiceAd;
using MudBlazor;
using System.Text.Json;
using System.Net;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;
using System.Threading.Tasks;

namespace QLN.ContentBO.WebUI.Pages.Services
{
    public partial class P2PListingTableBase : ComponentBase
    {
        [Inject] public IDialogService DialogService { get; set; }
        [Inject] public IServiceBOService _serviceBOService { get; set; }
        [Inject] public NavigationManager Navigation { get; set; }
        [Parameter] public EventCallback<int> OnPageChange { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Parameter] public ItemEditAdPost AdModel { get; set; } = new();
        [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }
         [Parameter] public EventCallback OnListReload { get; set; }
        public string? rejectReason { get; set; } = string.Empty;
        public long selectedAdId { get; set; }
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
            SelectedListings.Clear();
            int? status = newTab switch
            {
                "pendingApproval" => 2,
                "published" => 3,
                "unpublished" => 4,
                "promoted" => 7,
                "featured" => 9,
                _ => null
            };
            await OnStatusChanged.InvokeAsync(status);
        }
        private async Task HandleRejection(string reason)
        {
            rejectReason = string.IsNullOrWhiteSpace(reason) ? null : reason;
            await TriggerUpdate(BulkModerationAction.Remove);
        }
        private async Task HandleNeedChnage(string reason)
        {
            rejectReason = string.IsNullOrWhiteSpace(reason) ? null : reason;
            await TriggerUpdate(BulkModerationAction.NeedChanges);
        }
        protected Task RequestChanges(ServiceAdSummaryDto item)
        {
            OpenRejectDialog();
            return Task.CompletedTask;
        }
        protected void OpenRejectDialog()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Remove Verification" },
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
        protected void OpenNeedChangeDialog()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Need Change Request" },
                { "Description", "Please enter a reason before requesting Change" },
                { "ButtonTitle", "Need Change" },
                { "OnRejected", EventCallback.Factory.Create<string>(this, HandleNeedChnage) }
            };
            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };
            var dialog = DialogService.Show<RejectVerificationDialog>("", parameters, options);
        }
        protected async Task TriggerUpdate(BulkModerationAction status)
        {
            var request = new BulkModerationRequest
            {
                Action = status,
                AdIds = SelectedListings.Select(x => x.Id).ToList(),
            };
            if (status == BulkModerationAction.Remove || status == BulkModerationAction.NeedChanges && !string.IsNullOrWhiteSpace(rejectReason))
            {
                request.Reason = rejectReason;
            }
            await UpdateStatus(request);
        }
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
        public void OnEdit(ServiceAdSummaryDto item)
        {
            Navigation.NavigateTo($"/manage/services/editform/{item.Id}/p2plistings");
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
        protected async Task OpenPreviewDialog(ServiceAdSummaryDto source)
        {
            AdModel = MapToItemEditAdPost(source);
            var parameters = new DialogParameters { { "AdModel", AdModel } };
            var options = new DialogOptions
            {
                FullScreen = true,
                CloseButton = true,
                MaxWidth = MaxWidth.ExtraLarge,
            };
            var dialog = DialogService.Show<PreviewAd>("Ad Preview", parameters, options);
            await dialog.Result;
        }
        public ItemEditAdPost MapToItemEditAdPost(ServiceAdSummaryDto source)
        {
            var item = new ItemEditAdPost
            {
                Id = source.Id,
                UserId = source.UserId,
                UserName = source.UserName,
                Title = source.AdTitle,
                Status = (int?)source.Status,
                IsPromoted = source.IsPromoted ?? false,
                IsFeatured = source.IsFeatured ?? false,
                CreatedBy = source.UserName,
                CreatedAt = source.CreationDate,
                RefreshExpiryDate = source.DateExpiry,
                Images = source.ImageUpload != null
                    ? source.ImageUpload.Select(img => new AdImage
                    {
                        Id = img.Id ?? Guid.NewGuid(),
                        Url = img.Url ?? string.Empty,
                        AdImageFileName = System.IO.Path.GetFileName(img.Url ?? string.Empty),
                        Order = img.Order
                    }).ToList()
                    : new List<AdImage>()
            };

            return item;
        }


        protected async Task UpdateStatus(BulkModerationRequest statusRequest)
        {
            var response = await _serviceBOService.UpdateServiceStatus(statusRequest);
            if (response.IsSuccessStatusCode)
            {
                var status = statusRequest.Action switch
                {
                    BulkModerationAction.Approve => ServiceStatus.Published,
                    BulkModerationAction.Publish => ServiceStatus.Published,
                    BulkModerationAction.Unpublish => ServiceStatus.Unpublished,
                    BulkModerationAction.Remove => ServiceStatus.Rejected,
                    BulkModerationAction.NeedChanges => ServiceStatus.NeedsModification,
                    _ => ServiceStatus.Draft
                };

                Snackbar.Add(
                statusRequest.Action switch
                {
                    BulkModerationAction.Approve => "Service Ad Approved Successfully",
                    BulkModerationAction.Publish => "Service Ad Published Successfully",
                    BulkModerationAction.Unpublish => "Service Ad Unpublished Successfully",
                    BulkModerationAction.Remove => "Service Ad Removed Successfully",
                    BulkModerationAction.NeedChanges => "Requested for Need Change Successfully",
                    _ => "Service Ad Updated Successfully"
                },
                    Severity.Success
                );
                int? statusResult = selectedTab switch
                {
                    "pendingApproval" => 2,
                    "published" => 3,
                    "unpublished" => 4,
                    "Promoted" => 7,
                    "featured" => 9,
                    _ => null
                };
                await OnStatusChanged.InvokeAsync(statusResult);
                SelectedListings.Clear();
                StateHasChanged();
            }
            else if (response.StatusCode == HttpStatusCode.Conflict)
            {
                Snackbar.Add("You already have an active ad in this category. Please unpublish or remove it before posting another.", Severity.Error);
            }
            else
            {
                Snackbar.Add("Failed to update ad status", Severity.Error);
            }
        }
        protected void OpenRejectDialog(long guid)
        {
            selectedAdId = guid;
            var parameters = new DialogParameters
            {
                { "Title", "Remove Subscription" },
                { "Description", "Please enter a reason before removing" },
                { "ButtonTitle", "Remove" },
                { "OnRejected", EventCallback.Factory.Create<string>(this, HandleRemove) }
            };
            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };
            var dialog = DialogService.Show<RejectVerificationDialog>("", parameters, options);
        }
        private async Task HandleRemove(string reason)
        {
            rejectReason = string.IsNullOrWhiteSpace(reason) ? null : reason;
            await RemoveSubscription();
        }
        protected async Task RemoveSubscription()
        {
            var statusRequest = new BulkModerationRequest
            {
                AdIds = new List<long> { selectedAdId },
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