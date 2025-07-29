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
        [Parameter] public bool IsLoading { get; set; } = false;
        [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;

         [Parameter] public EventCallback OnConfirmed { get; set; }

        protected string HeaderIconPath { get; set; } = "/qln-images/success_icon.svg";

        protected override void OnInitialized()
        {
            HeaderIconPath = Title.Contains("delete", StringComparison.OrdinalIgnoreCase)
                ? "/qln-images/waring_icon.svg"
                : "/qln-images/success_icon.svg";
        }
        public void Cancel() => MudDialog.Cancel();
         public async Task Confirm()
        {
            IsLoading = true;
            StateHasChanged();

            if (OnConfirmed.HasDelegate)
                await OnConfirmed.InvokeAsync();

            MudDialog.Close(DialogResult.Ok(true));
        }
       
    }
}

