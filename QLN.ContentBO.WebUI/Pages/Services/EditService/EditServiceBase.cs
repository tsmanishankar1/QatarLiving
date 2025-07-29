using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using Microsoft.AspNetCore.WebUtilities;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.Services.EditService
{
    public class EditServiceBase : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; }
        [Inject] public IDialogService DialogService { get; set; }

        protected void GoBack()
        {
            Navigation.NavigateTo("/manage/services/listing");
        }
        protected AdPost adPostModel { get; set; } = new();

        protected string? Id { get; set; }

        protected override void OnInitialized()
        {
            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("id", out var id))
            {
                Id = id;
            }
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
        protected async Task OpenNeedChangesDialog()
        {
            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };
            var dialog = DialogService.Show<CommentDialog>("", options: options);
            var result = await dialog.Result;
            if (!result.Canceled)
            {
                string userComment = result.Data?.ToString() ?? "";
            }
            else
            {
                Console.WriteLine("User skipped the dialog.");
            }
    
        }
    
    }
}
