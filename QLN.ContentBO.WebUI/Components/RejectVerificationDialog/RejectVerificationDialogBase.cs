using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Components.RejectVerificationDialog
{
    public class RejectVerificationDialogBase : ComponentBase
    {
        [Parameter] public string Title { get; set; } = "Reject verification";
        [Parameter] public string Description { get; set; } = "Please enter a reason before rejecting";
        [Parameter] public string ButtonTitle { get; set; } = "Reject";
        protected bool IsLoading { get; set; } = false;
        protected bool IsConfirmDisabled => IsLoading || string.IsNullOrWhiteSpace(Reason);

        [Parameter] public EventCallback<string> OnRejected { get; set; }

        [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; } = default!;

        public string Reason { get; set; } = string.Empty;

        public void Cancel() => MudDialog.Cancel();

        public async Task Confirm()
        {
            if (!string.IsNullOrWhiteSpace(Reason) && OnRejected.HasDelegate)
            {
                IsLoading = true;
                StateHasChanged();

                await OnRejected.InvokeAsync(Reason);

                MudDialog.Close(DialogResult.Ok(Reason));
            }
        }
    }
}
