using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Interfaces;

using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;
using QLN.ContentBO.WebUI.Pages.Services.PreviewServiceAd;

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
         [Inject] ILogger<ViewSubscriptionListingTableBase> Logger { get; set; }
        [Parameter] public ItemEditAdPost AdModel { get; set; } = new();
        public ServicesDto selectedService { get; set; } = new ServicesDto();
        public string? rejectReason { get; set; } = string.Empty;
        public long selectedAdId { get; set; }
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
            Navigation.NavigateTo($"/manage/services/editform/{item.AddId}/subscription");
        }
        public async Task OnPreview(long Id)
        {
            selectedService = await GetServiceById(Id);
            await OpenPreviewDialog(selectedService);
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
        protected void OpenRejectDialog(long guid)
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
        protected async Task OpenPreviewDialog(ServicesDto source)
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
        public ItemEditAdPost MapToItemEditAdPost(ServicesDto source)
        {
            var item = new ItemEditAdPost
            {
                Id = source.Id,
                UserId = source.UserName, 
                UserName = source.UserName,
                Title = source.Title,
                Status = (int?)source.Status,
                IsPromoted = source.IsPromoted,
                IsFeatured = source.IsFeatured,
                CreatedBy = source.CreatedBy,
                CreatedAt = source.CreatedAt,
                RefreshExpiryDate = source.ExpiryDate,
                Images = source.PhotoUpload != null
                    ? source.PhotoUpload.Select(img => new AdImage
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
        private async Task HandleRejection(string reason)
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
        protected async Task<ServicesDto> GetServiceById(long Id)
        {
            try
            {
                var apiResponse = await _serviceBOService.GetServiceById(Id);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<ServicesDto>();
                    return response ?? new ServicesDto();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventsLocations");
            }
            return new ServicesDto();
        }

    }
}