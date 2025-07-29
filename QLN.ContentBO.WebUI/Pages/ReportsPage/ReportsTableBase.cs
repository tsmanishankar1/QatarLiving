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

        private string _ignoreDescription = string.Empty;
        private string _deleteDescription = string.Empty;
        protected override void OnInitialized()
        {
            switch (Type)
            {
                case "article-comments":
                    _ignoreDescription = "Are you sure you want to ignore this article comment report?";
                    _deleteDescription = "Are you sure you want to delete this reported article comment? This action cannot be undone.";
                    break;

                case "community-posts":
                    _ignoreDescription = "Are you sure you want to ignore this community post report?";
                    _deleteDescription = "Are you sure you want to delete this reported community post? This action cannot be undone.";
                    break;

                case "community-comments":
                    _ignoreDescription = "Are you sure you want to ignore this community comment report?";
                    _deleteDescription = "Are you sure you want to delete this reported community comment? This action cannot be undone.";
                    break;

                default:
                    _ignoreDescription = "Are you sure you want to ignore this report?";
                    _deleteDescription = "Are you sure you want to delete this report? This action cannot be undone.";
                    break;
            }
        }
        protected async Task ShowConfirmationDialog(Guid id, bool isIgnore)
        {
            string title = isIgnore ? "Ignore Confirmation" : "Delete Confirmation";
            string buttonTitle = isIgnore ? "Ignore" : "Delete";
            string description = isIgnore ? _ignoreDescription : _deleteDescription;

            var parameters = new DialogParameters
        {
            { "Title", title },
            { "Descrption", description },
            { "ButtonTitle",buttonTitle},
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
