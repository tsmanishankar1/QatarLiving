using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;

namespace QLN.ContentBO.WebUI.Pages.Classified.DealsMenu.EditCompany
{
    public class EditAdActionBase : ComponentBase
    {
        [Inject] public IDialogService DialogService { get; set; }
        [Parameter] public CompanyProfileItem Company { get; set; } = new();
        protected VerifiedStatus CurrentStatus => 
        (VerifiedStatus)(Company.CompanyVerificationStatus ?? (int)VerifiedStatus.Pending);
        [Parameter] public EventCallback<CompanyUpdateActions> OnCompanyAction { get; set; }
        protected async Task NeedChangesAsync()
        {
            await ShowReasonDialog(
                "Need Changes",
                "Please enter a reason for requesting changes.",
                "Submit",
                async (reason) =>
                {
                    var companyUpdateAction = new CompanyUpdateActions
                    {
                        CompanyId = Company.Id,
                        Status = (VerifiedStatus)Company.CompanyVerificationStatus.GetValueOrDefault(1), 
                        CompanyVerificationStatus = VerifiedStatus.NeedChanges,
                        Reason = reason
                    };
                    await OnCompanyAction.InvokeAsync(companyUpdateAction);
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

     
        protected async Task ShowConfirmation(string title, string message, string buttonTitle,VerifiedStatus action)
        {
            var companyUpdateAction = new CompanyUpdateActions
            {
                    CompanyId = Company.Id,
                    Status = (VerifiedStatus)Company.CompanyVerificationStatus.GetValueOrDefault(0), 
                    CompanyVerificationStatus = action,
            };
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Descrption", message },
                { "ButtonTitle", buttonTitle },
                { "OnConfirmed", EventCallback.Factory.Create(this, async () => {
                    await OnCompanyAction.InvokeAsync(companyUpdateAction);
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
