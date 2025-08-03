using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.Classified.PreLoved.UserProfile.VerificationPreview
{
    public class VerificationActionsBase : ComponentBase
    {
        [Inject] public IDialogService DialogService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        protected void GoBack()
        {
            NavigationManager.NavigateTo("/manage/classified/collectibles/user/verification/profile");
        }


        protected async Task ApproveAsync()
        {
            await ShowConfirmation(
                "Approve Verification",
                "Are you sure you want to approve this request?",
                "Approve",
                async () =>
                {
                    Console.WriteLine("✅ Approved");
                    // Your approve logic
                    await Task.CompletedTask;
                });
        }

        protected async Task NeedChangesAsync()
        {
            await ShowReasonDialog(
                "Need Changes",
                "Please enter a reason for requesting changes.",
                "Submit",
                async (reason) =>
                {
                    Console.WriteLine($"✏️ Need Changes Reason: {reason}");
                    // Your need changes logic
                    await Task.CompletedTask;
                });
        }

        protected async Task RejectAsync()
        {
            await ShowReasonDialog(
                "Reject Verification",
                "Please enter a reason before rejecting.",
                "Reject",
                async (reason) =>
                {
                    Console.WriteLine($"❌ Rejected Reason: {reason}");
                    // Your reject logic
                    await Task.CompletedTask;
                });
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
            await dialog.Result;
        }

        protected async Task ShowReasonDialog(string title, string description, string buttonTitle, Func<string, Task> onReasonSubmitted)
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Description", description },
                { "ButtonTitle", buttonTitle },
                { "OnRejected", EventCallback.Factory.Create<string>(this, onReasonSubmitted) }
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            DialogService.Show<RejectVerificationDialog>("", parameters, options);
        }
    }
}
