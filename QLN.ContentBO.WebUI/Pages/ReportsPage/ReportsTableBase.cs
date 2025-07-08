using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.ReportsPage
{
    public class ReportsTableBase : QLComponentBase
    {
        [Parameter] public List<ReportDto> Posts { get; set; } = new();
        [Parameter] public EventCallback<Guid> OnIgnore { get; set; }
        [Parameter] public EventCallback<Guid> OnDelete { get; set; }
        [Parameter] public string Type { get; set; } = string.Empty;
        [Parameter] public bool IsLoading { get; set; }
        [Inject] public IDialogService DialogService { get; set; }

        protected async Task ShowConfirmationDialog(Guid id, bool isIgnore)
        {
            var parameters = new DialogParameters
        {
            { "Title", isIgnore ? "Ignore Confirmation" : "Delete Confirmation" },
            { "Descrption", isIgnore ? "Do you want to ignore this report?" : "Do you want to delete this report?" },
            { "ButtonTitle", isIgnore ? "Ignore" : "Delete" },
            {
                "OnConfirmed", EventCallback.Factory.Create(this, async () =>
                {
                    if (isIgnore)
                        await OnIgnore.InvokeAsync(id);
                    else
                        await OnDelete.InvokeAsync(id);
                })
            }
        };

            var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = DialogService.Show<ConfirmationDialog>("", parameters, options);
            var result = await dialog.Result;
        }
    }
}
