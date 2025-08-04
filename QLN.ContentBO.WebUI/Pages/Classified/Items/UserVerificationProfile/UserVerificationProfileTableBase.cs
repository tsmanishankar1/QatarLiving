using Microsoft.AspNetCore.Components;
using System;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Components.AdHistoryDialog;
using System.Collections.Generic;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.UserVerificationProfile
{
    public partial class ViewTransactionsTableBase : ComponentBase
    {
        protected List<BusinessVerificationItem> Listings { get; set; } = new();
        [Inject]
        protected NavigationManager Navigation { get; set; }
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected int TotalCount => Listings.Count;
        protected void HandlePageChange(int newPage)
        {
            currentPage = newPage;
            StateHasChanged();
        }

        protected void HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            currentPage = 1; // reset to first page
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
                    UserId = "101",
                    BusinessName = "Beethoven's",
                    UserName = "Rashid",
                    CRFile = "PDF",
                    CRLicense = "446558",
                    EndDate = DateTime.Parse("2025-12-05")
                },
                new BusinessVerificationItem
                {
                    UserId = "102",
                    BusinessName = "LEGO® Shows 2025",
                    UserName = "LEGO® Shows 2",
                    CRFile = "N/A",
                    CRLicense = "446558",
                    EndDate = DateTime.Parse("2025-12-05")
                },
                new BusinessVerificationItem
                {
                    UserId = "103",
                    BusinessName = "Tech Galaxy",
                    UserName = "Ayaan Khan",
                    CRFile = "DOCX",
                    CRLicense = "992134",
                    EndDate = DateTime.Parse("2025-11-20")
                },
                new BusinessVerificationItem
                {
                    UserId = "104",
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
