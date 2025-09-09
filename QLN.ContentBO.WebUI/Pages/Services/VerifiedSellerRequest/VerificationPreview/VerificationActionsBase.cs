using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.Services.VerifiedSellerRequest.VerificationPreview
{
    public class VerificationActionsBase : ComponentBase
    {
         [Inject] public IDialogService DialogService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        [Parameter] public EventCallback<CompanyUpdateActions> OnCompanyAction { get; set; }
        [Parameter]
        public CompanyProfileItem CompanyDetails { get; set; } = new();
        public VerifiedStatus CurrentStatus
        {
            get => (VerifiedStatus)(CompanyDetails?.CompanyVerificationStatus ?? (int)VerifiedStatus.Pending);
            set
            {
                if (CompanyDetails != null)
                    CompanyDetails.CompanyVerificationStatus = (int)value;
            }
        }

        protected void GoBack()
        {
            NavigationManager.NavigateTo("/manage/classified/items/user/verification/profile");
        }
        protected async Task ApproveAsync()
        {
            var companyUpdateAction = new CompanyUpdateActions
            {
                CompanyId = CompanyDetails.Id, 
                Status = (VerifiedStatus)CompanyDetails.CompanyVerificationStatus,
                CompanyVerificationStatus = VerifiedStatus.Approved 
            };
            await ShowConfirmation(
                "Approve Verification",
                "Are you sure you want to approve this User Profile?",
                "Approve",
                async () =>
                {
                    await OnCompanyAction.InvokeAsync(companyUpdateAction);
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
                    var companyUpdateAction = new CompanyUpdateActions
                    {
                        CompanyId = CompanyDetails.Id, 
                        Status = (VerifiedStatus)CompanyDetails.CompanyVerificationStatus,
                        CompanyVerificationStatus = VerifiedStatus.NeedChanges 
                    };
                   await OnCompanyAction.InvokeAsync(companyUpdateAction);
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
                     var companyUpdateAction = new CompanyUpdateActions
                    {
                        CompanyId = CompanyDetails.Id, 
                        Status = (VerifiedStatus)CompanyDetails.CompanyVerificationStatus,
                        CompanyVerificationStatus = VerifiedStatus.Rejected
                    };
                   await OnCompanyAction.InvokeAsync(companyUpdateAction);
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
