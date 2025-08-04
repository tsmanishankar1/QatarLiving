using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Threading.Tasks;

namespace QLN.ContentBO.WebUI.Components.DiscardDialog
{
    public class DiscardDialogBase : ComponentBase
    {
        [CascadingParameter] 
        protected IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] 
        public string Title { get; set; } = "Discard";

        [Parameter] 
        public string Description { get; set; } = "Are you sure you want to discard changes?";

        [Parameter] 
        public EventCallback OnDiscard { get; set; }

        protected void Cancel() => MudDialog.Cancel();

        protected async Task OnDiscardClicked()
        {
            if (OnDiscard.HasDelegate)
                await OnDiscard.InvokeAsync();

            MudDialog.Close(DialogResult.Ok(true));
        }
    }
}
