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


        protected override void OnInitialized()
        {
            Listings = GetSampleData();
        }
        private List<BusinessVerificationItem> GetSampleData()
        {
            return new List<BusinessVerificationItem>
            {
                new BusinessVerificationItem
                {
                    UserId = 101,
                    BusinessName = "Beethoven's",
                    UserName = "Rashid",
                    CRFile = "PDF",
                    CRLicense = "446558",
                    EndDate = DateTime.Parse("2025-12-05")
                },
                new BusinessVerificationItem
                {
                    UserId = 102,
                    BusinessName = "LEGO® Shows 2025",
                    UserName = "LEGO® Shows 2",
                    CRFile = "N/A",
                    CRLicense = "446558",
                    EndDate = DateTime.Parse("2025-12-05")
                },
                new BusinessVerificationItem
                {
                    UserId = 103,
                    BusinessName = "Tech Galaxy",
                    UserName = "Ayaan Khan",
                    CRFile = "DOCX",
                    CRLicense = "992134",
                    EndDate = DateTime.Parse("2025-11-20")
                },
                new BusinessVerificationItem
                {
                    UserId = 104,
                    BusinessName = "Sunrise Traders",
                    UserName = "Meera Sharma",
                    CRFile = "PDF",
                    CRLicense = "781245",
                    EndDate = DateTime.Parse("2025-10-15")
                }
            };
        }

        protected void ShowPreview(BusinessVerificationItem item)
        {
            Navigation.NavigateTo($"/manage/classified/items/verification/preview/{item.UserId}");
        }
    }
}
