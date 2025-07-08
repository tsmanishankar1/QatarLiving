using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Net;

namespace QLN.ContentBO.WebUI.Components.ConfirmationDialog
{
    public class ConfirmationeDialogBase : QLComponentBase
    {
        [Parameter] public string Title { get; set; } = "Article Action";
        [Parameter] public string Descrption { get; set; } = "Article Action";
        [Parameter] public string ButtonTitle { get; set; } = "Article Action";
        [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
         [Parameter] public EventCallback OnConfirmed { get; set; }
        public void Cancel() => MudDialog.Cancel();
         public async Task Confirm()
        {
            if (OnConfirmed.HasDelegate)
                await OnConfirmed.InvokeAsync();
            MudDialog.Close(DialogResult.Ok(true));
        }
    }
}
