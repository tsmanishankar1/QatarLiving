using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;

namespace QLN.ContentBO.WebUI.Pages.Services
{
    public partial class ViewSubscriptionListingTableBase : ComponentBase
    {
        [Inject] public IDialogService DialogService { get; set; }
        [Inject] public NavigationManager Navigation { get; set; }
        protected HashSet<ServiceAdPaymentSummaryDto> SelectedListings { get; set; } = new();
         [Parameter] public EventCallback<int> OnPageChange { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected int TotalCount => Listings.Count;
        [Parameter]
        public List<ServiceAdPaymentSummaryDto> Listings { get; set; } = new();
        protected void HandlePageChange(int newPage)
        {
            currentPage = newPage;
            StateHasChanged();
        }

        protected void HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            currentPage = 1;
            StateHasChanged();
        }
        public void OnEdit(ServiceAdPaymentSummaryDto item)
        {
            Navigation.NavigateTo("/manage/services/editform");
        }
        public void OnPreview(ServiceAdPaymentSummaryDto item)
        {
        }
        protected async Task ShowConfirmation(string title, string description, string buttonTitle, Func<Task> onConfirmedAction)
        {
            var parameters = new DialogParameters
        {
            { "Title", title },
            { "Descrption", description },
            { "ButtonTitle", buttonTitle },
            { "OnConfirmed", EventCallback.Factory.Create(this, onConfirmedAction) }
        };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = DialogService.Show<ConfirmationDialog>("", parameters, options);
            var result = await dialog.Result;
        }
    }
}