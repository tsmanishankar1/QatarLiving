using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;

namespace QLN.ContentBO.WebUI.Pages.Services
{
    public partial class VerifiedSellerRequestTableBase : ComponentBase
    {
        [Inject] public IDialogService DialogService { get; set; }
        [Inject] public NavigationManager Navigation { get; set; }
        [Parameter] public EventCallback<int> OnPageChange { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }
        [Parameter]
        public EventCallback<int?> OnStatusChanged { get; set; }
        protected HashSet<VerificationProfileStatus> SelectedListings { get; set; } = new();
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected string selectedTab = "verificationRequests";
        protected int TotalCount => Listings.Count;
        [Parameter]
        public List<VerificationProfileStatus> Listings { get; set; } = new();
        protected List<ToggleTabs.TabOption> tabOptions = new()
        {
            new() { Label = "Verification Requests", Value = "verificationRequests" },
            new() { Label = "Rejected", Value = "rejected" },
            new() { Label = "Approved", Value = "approved" }
        };
        protected async Task OnTabChanged(string newTab)
        {
            selectedTab = newTab;

            int? status = newTab switch
            {
                "verificationRequests" => 1,
                "rejected" => 4,
                "approved" => 8,
                _ => null
            };
            await OnStatusChanged.InvokeAsync(status); 
        }
        protected async void HandlePageChange(int newPage)
        {
            currentPage = newPage;
            await OnPageChange.InvokeAsync(currentPage);
        }

        protected async void HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            currentPage = 1;
            await OnPageSizeChange.InvokeAsync(pageSize);
        }
        public void OnEdit(VerificationProfileStatus item)
        {
            Navigation.NavigateTo($"/manage/services/editform/{item.Id}");
        }
        public void OnPreview(VerificationProfileStatus item)
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