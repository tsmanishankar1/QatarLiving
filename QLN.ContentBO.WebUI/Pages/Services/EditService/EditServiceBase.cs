using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using PSC.Blazor.Components.MarkdownEditor;
using PSC.Blazor.Components.MarkdownEditor.EventsArgs;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using MudBlazor;
using System.Net;
using QLN.ContentBO.WebUI.Interfaces;

namespace QLN.ContentBO.WebUI.Pages.Services.EditService
{
    public class EditServiceBase : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; }
        [Inject] public IDialogService DialogService { get; set; }
        [Inject] IServiceBOService serviceBOService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        protected string UploadImageButtonName { get; set; } = "uploadImage";
        protected MudFileUpload<IBrowserFile> _markdownfileUploadRef;
        public string? Reason { get; set; }
        protected string[] HiddenIcons = ["fullscreen"];
        protected MarkdownEditor MarkdownEditorRef;
        [Inject] ILogger<EditServiceBase> Logger { get; set; }
        private BulkModerationAction _selectedAction;
        protected AdPost adPostModel { get; set; } = new();
        [Parameter]
        public long? Id { get; set; }
        [Parameter]
        public string? Source { get; set; }
        public ServicesDto selectedService { get; set; } = new ServicesDto();
        protected override async Task OnParametersSetAsync()
        {
            try
            {
                if (Id.HasValue)
                {
                    selectedService = await GetServiceById(Id.Value);
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnParametersSetAsync");
            }
        }
        protected void GoBack()
        {
            switch (Source?.ToLower())
            {
                case "subscription":
                    Navigation.NavigateTo("manage/services/listing/subscriptions");
                    break;
                case "p2plistings":
                    Navigation.NavigateTo("manage/services/listing/p2p/listing");
                    break;
                case "p2ptransactions":
                    Navigation.NavigateTo("manage/services/listing/p2p/transactions");
                    break;
                case "subscriptionads":
                    Navigation.NavigateTo("manage/services/listing/subscription/ads");
                    break;
                default:
                    Navigation.NavigateTo("manage/services/listing/subscriptions");
                    break;
            }
        }
        private async Task<ServicesDto> GetServiceById(long Id)
        {
            try
            {
                var apiResponse = await serviceBOService.GetServiceById(Id);
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
        protected async Task ShowConfirmation(string title, string message, string buttonTitle, BulkModerationAction status)
        {
            _selectedAction = status;
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Descrption", message },
                { "ButtonTitle", buttonTitle },
                { "OnConfirmed", EventCallback.Factory.Create(this, async () => {
                    await UpdateStatus();
                })}
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = DialogService.Show<ConfirmationDialog>("", parameters, options);
            await dialog.Result;
        }
        private async Task UpdateStatus()
        {
            var request = new BulkModerationRequest
            {
                AdIds = new List<long> { selectedService.Id },
                Action = _selectedAction,
                Reason = null,
            };
            var response = await serviceBOService.UpdateServiceStatus(request);
            if (response.IsSuccessStatusCode)
            {
                    var status = _selectedAction switch
                    {
                        BulkModerationAction.Approve => ServiceStatus.Published,
                        BulkModerationAction.Publish => ServiceStatus.Published,
                        BulkModerationAction.Unpublish => ServiceStatus.Unpublished,
                        BulkModerationAction.Remove => ServiceStatus.Rejected,
                        BulkModerationAction.NeedChanges => ServiceStatus.NeedsModification,
                        _ => selectedService.Status
                    };
                    selectedService.Status = status;
                     var isFeatured = _selectedAction switch
                     {
                         BulkModerationAction.Feature => true,
                         BulkModerationAction.UnFeature => false,
                         _ => selectedService.IsFeatured
                    };
                    selectedService.IsFeatured = isFeatured;
                     var isPromoted = _selectedAction switch
                     {
                         BulkModerationAction.Promote => true,
                         BulkModerationAction.UnPromote => false,
                         _ => selectedService.IsPromoted
                    };
                    selectedService.IsPromoted = isPromoted;

                Snackbar.Add(
                _selectedAction switch
                {
                    BulkModerationAction.Approve => "Service Ad Approved Successfully",
                    BulkModerationAction.Publish => "Service Ad Published Successfully",
                    BulkModerationAction.Unpublish => "Service Ad Unpublished Successfully",
                    BulkModerationAction.Remove => "Service Ad Removed Successfully",
                    BulkModerationAction.UnPromote => "Service Ad Un promoted Successfully",
                    BulkModerationAction.Feature => "Service Ad Featured Successfully",
                    BulkModerationAction.Promote => "Service Ad Un promoted Successfully",
                    BulkModerationAction.UnFeature => "Service Ad Un Featured Successfully",
                    BulkModerationAction.NeedChanges => "Requested for Change Successfully",
                    _ => "Service Ad Updated Successfully"
                },
                    Severity.Success
                );
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

        protected async Task OpenNeedChangesDialog()
        {
            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };
            var parameters = new DialogParameters
            {
                { "ActionType", BulkModerationAction.NeedChanges } 
            };
            var dialog = DialogService.Show<CommentDialog>("", parameters, options);
            var result = await dialog.Result;
            if (!result.Canceled)
            {
                Reason = result.Data?.ToString() ?? "";
            }
            await ShowConfirmation("Need Changes", "Are you sure you want to Request For a Change?", "Request", BulkModerationAction.NeedChanges);
        }
        protected async Task OpenFeatureUnfeatureDialog(BulkModerationAction status)
        {
            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };
            var parameters = new DialogParameters
            {
                { "ActionType", status } 
            };
            var dialog = DialogService.Show<CommentDialog>("", parameters, options);
            var result = await dialog.Result;
            if (!result.Canceled)
            {
                Reason = result.Data?.ToString() ?? "";
            }
            if (status == BulkModerationAction.Feature)
            {
                await ShowConfirmation("Feature Ad", "Are you sure you want to Feature This Ad?", "Feature", BulkModerationAction.Feature);
            }
            else if (status == BulkModerationAction.Promote)
            { 
                await ShowConfirmation("Promote Ad", "Are you sure you want to Promote This Ad?", "Promote", BulkModerationAction.Promote);

            }
        }
        protected Task OnCustomButtonClicked(MarkdownButtonEventArgs eventArgs)
        {
            if (eventArgs.Name is not null)
            {
                if (eventArgs.Name == UploadImageButtonName)
                {
                    TriggerCustomImageUpload();
                }

                if (eventArgs.Name == "CustomPreview")
                {
                    ToggleMarkdownPreview();
                }
            }
            return Task.CompletedTask;
        }
        protected async void ToggleMarkdownPreview()
        {
            if (MarkdownEditorRef != null)
            {
                await MarkdownEditorRef.TogglePreviewAsync();
            }
        }
        protected void TriggerCustomImageUpload()
        {
            _markdownfileUploadRef.OpenFilePickerAsync();
        }

    }
}
