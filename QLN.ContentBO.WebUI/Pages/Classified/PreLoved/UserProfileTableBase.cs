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
        public List<BusinessVerificationItem> Listings { get; set; }
        [Parameter]
        public bool IsLoading { get; set; }
        [Parameter]
        public bool IsEmpty { get; set; }
        [Inject] public IDialogService DialogService { get; set; }

        [Parameter]
        public int TotalCount { get; set; }
        [Parameter]
        public EventCallback<int> OnPageChanged { get; set; }
        [Parameter]
        public string SelectedTab { get; set; }
        [Parameter]
        public EventCallback<int> OnPageSizeChanged { get; set; }
        [Parameter]
        public EventCallback<string> OnTabChanged { get; set; }
        protected string _activeTab;
        [Parameter]
        public EventCallback<string> selectedTabChanged { get; set; }

        protected async Task HandleTabChanged(string newTab)
        {
            if (_activeTab != newTab)
            {
                _activeTab = newTab;
                await OnTabChanged.InvokeAsync(newTab);
                await selectedTabChanged.InvokeAsync(newTab); // Notify parent of change
            }
        }
        protected string _activeProfileTab = ((int)VerifiedStatus.Pending).ToString();
        protected override void OnParametersSet()
        {
            Console.WriteLine($"Parent SelectedTab: {SelectedTab}");
            Console.WriteLine($"Child received selectedTab: {SelectedTab}");
            Console.WriteLine($"Received selectedTab: {SelectedTab}");
            if (!string.IsNullOrEmpty(SelectedTab))
            {
                _activeTab = SelectedTab;
            }
            else
            {
                _activeTab = ((int)AdStatus.PendingApproval).ToString();
            }
            Console.WriteLine($"Active tab set to: {_activeTab}");
        }
        protected List<ToggleTabs.TabOption> profileTabOptions = new()
        {
            new() { Label = "Verification Request", Value = ((int)VerifiedStatus.Pending).ToString() },
            new() { Label = "Approved", Value = ((int)VerifiedStatus.Approved).ToString() },
            new() { Label = "Rejected", Value = ((int)VerifiedStatus.Rejected).ToString() }
        };
        protected int currentPage = 1;
        protected int pageSize = 12;
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


       

        protected void ShowPreview(BusinessVerificationItem item)
        {
            Navigation.NavigateTo($"/manage/classified/preloved/verification/preview/{item.UserId}");
        }
    }
}
