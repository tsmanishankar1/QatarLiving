using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Components.AdHistoryDialog
{
    public class AdHistoryDialogBase : ComponentBase
    {
        [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public List<AdHistoryModel> AdHistoryList { get; set; } = new();

        protected void Close() => MudDialog?.Cancel();
    }

}