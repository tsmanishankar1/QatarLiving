using Microsoft.AspNetCore.Components;
using System;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;

namespace QLN.ContentBO.WebUI.Pages.Classified.PreLoved
{
    public partial class UserProfileTableBase : ComponentBase
    {
        [Inject]
        protected NavigationManager Navigation { get; set; }
       [Parameter]
        public List<CompanyProfileItem> Listings { get; set; } = new();
        [Parameter]
        public bool IsLoading { get; set; }
        [Parameter]
        public bool IsEmpty { get; set; }
        [Inject] public IDialogService DialogService { get; set; }

        [Parameter]
        public int TotalCount { get; set; }
        [Parameter]
        public EventCallback<int> OnPageChanged { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }
        [Parameter] public EventCallback<int> OnPageChange { get; set; }
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected async void HandlePageChange(int newPage)
        {
            currentPage = newPage;
            await OnPageChange.InvokeAsync(currentPage);
            StateHasChanged();
        }

        protected async void HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            await OnPageSizeChange.InvokeAsync(pageSize);
            StateHasChanged();
        }
        protected void ShowPreview(Guid id)
        {
            Navigation.NavigateTo($"/manage/classified/items/verification/preview/{id}");
        }
    }
}
