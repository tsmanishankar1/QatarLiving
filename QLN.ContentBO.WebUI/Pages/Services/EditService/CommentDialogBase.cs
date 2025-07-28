using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Extensions.Helper;

namespace QLN.ContentBO.WebUI.Pages.Services.EditService
{
    public class CommentDialogBase : ComponentBase
    {
        [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; } = default!;
        protected string Comments { get; set; } = string.Empty;

        protected void Save()
        {
            MudDialog.Close(DialogResult.Ok(Comments));
        }
        protected void Skip()
        {
            MudDialog.Cancel();
        }
    }
}