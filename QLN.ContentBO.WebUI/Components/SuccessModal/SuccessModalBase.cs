using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Components.SuccessModal
{
    public class SuccessModalBase : ComponentBase
    {
        [Parameter] public string Title { get; set; } = "Success!";
        [Parameter] public EventCallback OnClose { get; set; }

        [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; } = default!;

        protected async Task Close()
        {
            if (OnClose.HasDelegate)
                await OnClose.InvokeAsync();

            MudDialog.Close(DialogResult.Ok(true));
        }
    }
}
