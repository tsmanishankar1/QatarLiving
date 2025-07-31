using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;
using QLN.ContentBO.WebUI.Enums;
using System.Text.Json;
using QLN.ContentBO.WebUI.Interfaces;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.EditAd
{
    public class EditAdActionBase : ComponentBase
    {
        [Inject] public IDialogService DialogService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        [Inject] public IClassifiedService ClassifiedService { get; set; }
        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] public ILogger<EditAdActionBase> Logger { get; set; }

        [Parameter] public ItemEditAdPost AdModel { get; set; } = new();
        [Parameter] public int OrderId { get; set; } = 24578;

       protected bool CanPublish => AdModel?.Status == (int)AdStatus.Unpublished;
       protected bool CanUnpublish => AdModel?.Status == (int)AdStatus.Published;
       
         private void OpenRemoveReasonDialog()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Remove Ad" },
                { "Description", "Please enter a reason before removing." },
                { "ButtonTitle", "Remove" },
                { "OnRejected", EventCallback.Factory.Create<string>(this, async (reason) =>
                     await PerformSingleActionAsync(AdBulkActionType.Remove, reason)
                )}
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            DialogService.Show<RejectVerificationDialog>("", parameters, options);
        }


        protected async Task OpenPreviewDialog()
        {
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

       protected async Task ShowConfirmation(string title, string message, string buttonTitle)
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Descrption", message },
                { "ButtonTitle", buttonTitle },
                { "OnConfirmed", EventCallback.Factory.Create(this, async () => 
                    await HandleConfirmedAction(buttonTitle)
                )}
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
        private async Task HandleConfirmedAction(string buttonTitle)
        {
            var actionType = MapTitleToAction(buttonTitle);

            if (actionType == AdBulkActionType.Remove)
            {
                OpenRemoveReasonDialog(); // will call PerformSingleActionAsync later with reason
                return;
            }

            await PerformSingleActionAsync(actionType);
        }

        private AdBulkActionType MapTitleToAction(string title)
        {
            return title.ToLowerInvariant() switch
            {
                "feature" => AdBulkActionType.Feature,
                "unfeature" => AdBulkActionType.UnFeature,
                "promote" => AdBulkActionType.Promote,
                "unpromote" => AdBulkActionType.UnPromote,
                "publish" => AdBulkActionType.Publish,
                "unpublish" => AdBulkActionType.Unpublish,
                "remove" => AdBulkActionType.Remove,
                "refresh" => AdBulkActionType.Refresh,
                _ => throw new ArgumentException($"Unknown action title: {title}")
            };
        }
         private void UpdateAdModelState(AdBulkActionType actionType)
        {
            switch (actionType)
            {
                case AdBulkActionType.Publish:
                    AdModel.Status = 1;
                    break;
                case AdBulkActionType.Unpublish:
                    AdModel.Status = 4;
                    break;
                case AdBulkActionType.Promote:
                    AdModel.IsPromoted = true;
                    break;
                case AdBulkActionType.UnPromote:
                    AdModel.IsPromoted = false;
                    break;
                case AdBulkActionType.Feature:
                    AdModel.IsFeatured = true;
                    break;
                case AdBulkActionType.UnFeature:
                    AdModel.IsFeatured = false;
                    break;
                case AdBulkActionType.Refresh:
                    AdModel.IsRefreshed = true;
                    break;
            }
        }


      private async Task PerformSingleActionAsync(AdBulkActionType actionType, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(AdModel?.Id))
        {
            Snackbar.Add("Ad ID is missing.", Severity.Warning);
            return;
        }

        var payload = new Dictionary<string, object>
        {
            ["adIds"] = new List<string> { AdModel.Id },
            ["action"] = (int)actionType,
            ["reason"] = reason ?? string.Empty
        };

        try
        {
            var payloadJson = JsonSerializer.Serialize(payload);
            Logger.LogInformation("Performing single action: {Payload}", payloadJson);

            var response = await ClassifiedService.PerformBulkActionAsync("bulk-items-action", payload);

            if (response?.IsSuccessStatusCode == true)
            {
                Snackbar.Add($"Ad {actionType} successfully.", Severity.Success);
                if (actionType == AdBulkActionType.Remove)
                {
                    // Redirect after successful remove
                    NavigationManager.NavigateTo("/manage/classified/items/view/listing");
                    return;
                }
                UpdateAdModelState(actionType);
                StateHasChanged();
            }
            else
            {
                Snackbar.Add("Something went wrong while performing the action.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error performing single ad action");
            Snackbar.Add("Unexpected error occurred.", Severity.Error);
        }
    }

    }
}
