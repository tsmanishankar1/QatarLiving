using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.EditAd
{
    public class EditAdActionBase : ComponentBase
    {
        [Inject] public IDialogService DialogService { get; set; }
        [Parameter] public string UserName { get; set; } = "Rashid";
        [Parameter] public string Category { get; set; } = "Electronics";
        [Parameter] public int AdId { get; set; } = 21660;
        [Parameter] public int OrderId { get; set; } = 24578;

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
