using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.DealsMenu.EditAd
{
    public class EditDealsActionBase : QLComponentBase
    {
        [Inject] public IDialogService DialogService { get; set; }
        [Parameter] public DealsModal AdModel { get; set; } = new();
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

            var dialog = await DialogService.ShowAsync<PreviewDeals>("Ad Preview", parameters, options);
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
                    Console.WriteLine($"{buttonTitle} confirmed.");
                })}
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<ConfirmationDialog>("", parameters, options);
            await dialog.Result;
        }
    }
}
