using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Interfaces;
using System;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Components.AdHistoryDialog;
using System.Collections.Generic;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.Services.VerifiedSellerRequest
{
    public partial class UserVerificationProfileTableBase : ComponentBase
    {
        [Inject]
        protected NavigationManager Navigation { get; set; }
        [Parameter]
        public List<CompanyProfileItem> Listings { get; set; } = new();
        [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }
        [Parameter] public EventCallback<int> OnPageChange { get; set; }
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected int TotalCount => Listings.Count;
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
            Navigation.NavigateTo($"/manage/services/userprofile/preview/{id}");
        }
    
    }
}
