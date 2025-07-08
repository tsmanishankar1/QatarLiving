using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Community
{
    public class CommunityTableBase : QLComponentBase
    {
        [Parameter]
        public List<CommunityPostDto> Posts { get; set; }

        [Parameter]
        public bool IsLoading { get; set; }
        [Parameter]
        public string DeletingId { get; set; }

        [Parameter]
        public EventCallback<string> OnDelete { get; set; }
        [Inject] public IDialogService DialogService { get; set; }

        protected async Task DeleteSlotHandler(string id)
        {
            var parameters = new DialogParameters
        {
            { "Title", "Delete Confirmation" },
            { "Descrption", "Do you want to delete this Community?" },
            { "ButtonTitle", "Delete" },
              { "OnConfirmed", EventCallback.Factory.Create(this, async () =>
                {
                    await OnDelete.InvokeAsync(id);
                })
            }
        };
            var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = DialogService.ShowAsync<ConfirmationDialog>("", parameters, options);
            var result = await dialog;
        }

    }
}
