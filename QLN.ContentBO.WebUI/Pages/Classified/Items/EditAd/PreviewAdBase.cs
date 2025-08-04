using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.EditAd
{
    public class PreviewAdBase : ComponentBase
    {
        [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public ItemEditAdPost AdModel { get; set; } = new();

        [Parameter] public string UserName { get; set; }
        [Parameter] public string Category { get; set; }
        [Parameter] public int AdId { get; set; }
        [Parameter] public int OrderId { get; set; }

        protected void CloseDialog()
        {
            MudDialog?.Close();
        }
    }
}
