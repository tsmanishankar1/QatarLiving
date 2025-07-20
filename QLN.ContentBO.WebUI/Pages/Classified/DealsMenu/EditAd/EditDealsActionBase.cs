using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;

namespace QLN.ContentBO.WebUI.Pages.Classified.DealsMenu.EditAd
{
    public class EditDealsActionBase : ComponentBase
    {
        [Inject] public IDialogService DialogService { get; set; }
        [Parameter] public EditAdPost AdModel { get; set; } = new();
        [Parameter] public string UserName { get; set; } = "Rashid";
        [Parameter] public string Category { get; set; } = "Electronics";
        [Parameter] public int AdId { get; set; } = 21660;
        [Parameter] public int OrderId { get; set; } = 24578;
        protected async Task OpenPreviewDialog()
        {
            var parameters = new DialogParameters
            {
                { "AdModel", AdModel } 
            };

            var options = new DialogOptions
            {
                FullScreen = true,
                CloseButton = true,
                MaxWidth = MaxWidth.ExtraLarge,
            };

            var dialog = DialogService.Show<PreviewDeals>("Ad Preview", parameters, options);
            await dialog.Result;
        }


        protected async Task ShowConfirmation(string title, string message, string buttonTitle)
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Descrption", message },
                { "ButtonTitle", buttonTitle },
                { "OnConfirmed", EventCallback.Factory.Create(this, async () => {
                    // Placeholder: handle actual action logic here.
                    Console.WriteLine($"{buttonTitle} confirmed.");
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
