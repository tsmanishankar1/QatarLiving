using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using Microsoft.AspNetCore.WebUtilities;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using MudBlazor;
using QLN.ContentBO.WebUI.Interfaces;

namespace QLN.ContentBO.WebUI.Pages.Services.EditService
{
    public class EditServiceBase : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; }
        [Inject] public IDialogService DialogService { get; set; }
        [Inject] IServiceBOService serviceBOService { get; set; }
        [Inject] ILogger<EditServiceBase> Logger { get; set; }

        protected void GoBack()
        {
            Navigation.NavigateTo("/manage/services/listing");
        }
        protected AdPost adPostModel { get; set; } = new();
        [Parameter]
        public Guid? Id { get; set; }
        public ServicesDto selectedService { get; set; } = new ServicesDto();
        protected override async Task OnParametersSetAsync()
        {
            try
            {
                if (Id.HasValue)
                {
                    selectedService = await GetServiceById(Id.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnParametersSetAsync");
            }
        }
        private async Task<ServicesDto> GetServiceById(Guid Id)
        {
            try
            {
                var apiResponse = await serviceBOService.GetServiceById(Id);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<ServicesDto>();
                    return response ?? new ServicesDto();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventsLocations");
            }
            return new ServicesDto();
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
