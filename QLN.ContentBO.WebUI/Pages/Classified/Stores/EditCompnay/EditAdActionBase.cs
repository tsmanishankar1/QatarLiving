using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.EditCompnay
{
    public class EditAdActionBase : ComponentBase
    {
        [Inject] public IDialogService DialogService { get; set; }
        [Parameter] public CompanyProfileItem Company { get; set; } = new();
        protected VerifiedStatus CurrentStatus => (VerifiedStatus)Company.Status;
        protected async Task NeedChangesAsync()
        {
            await ShowReasonDialog(
                "Need Changes",
                "Please enter a reason for requesting changes.",
                "Submit",
                async (reason) =>
                {
                    await Task.CompletedTask;
                });
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

     
        protected async Task ShowConfirmation(string title, string message, string buttonTitle)
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Descrption", message },
                { "ButtonTitle", buttonTitle },
                { "OnConfirmed", EventCallback.Factory.Create(this, async () => {
                    Console.WriteLine($"{buttonTitle} confirmed.");
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
    }
}
