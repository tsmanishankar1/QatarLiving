using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.UserVerificationProfile
{
    public partial class UserVerificationProfileBase : ComponentBase
    {
        protected string selectedTab = "verificationrequests";
         [Inject] IServiceBOService serviceBOService { get; set; }
        [Inject] public IClassifiedService ClassifiedService { get; set; }
        [Inject] private ILogger<UserVerificationProfileBase> Logger { get; set; } = default!;
        public List<CompanyProfileItem> CompanyProfileItems { get; set; } = new();
        public int? Status { get; set; }
         protected int currentPage = 1;
        protected int pageSize = 12;
        protected List<ToggleTabs.TabOption> tabOptions = new()
        {
            new() { Label = "Verification Requests", Value = "verificationrequests" },
            new() { Label = "Rejected", Value = "rejected" },
            new() { Label = "Approved", Value = "approved" },
        };
        protected override async Task OnInitializedAsync()
        {
            currentPage = 1;
            pageSize = 12;
            Status = 1;
            CompanyProfileItems = await GetCompanyProfiles();
        }
        protected async Task HandleStatusChange(int? status)
        {
            Status = status;
            CompanyProfileItems = await GetCompanyProfiles();
            StateHasChanged();
        }
        protected async Task HandlePageChange(int newPage)
        {
            currentPage = newPage;
            CompanyProfileItems = await GetCompanyProfiles();
            StateHasChanged();
        }
    
        protected async Task HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            CompanyProfileItems = await GetCompanyProfiles();
            StateHasChanged();
        }
        protected async Task OnTabChanged(string newTab)
        {
            selectedTab = newTab;

            Status = newTab switch
            {
                "verificationrequests" => 1,
                "rejected" => 4,
                "approved" => 2,
                _ => null
            };
            HandleStatusChange(Status);
            StateHasChanged();
        }
        private async Task<List<CompanyProfileItem>> GetCompanyProfiles()
        {
            try
            {
                var payload = new
                {
                    vertical = Vertical.Classifieds,
                    subVertical = SubVertical.Items,
                    companyVerificationStatus = 1,
                    pageNumber = currentPage,
                    pageSize = pageSize
                };
                var response = await serviceBOService.GetAllCompaniesAsync(payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CompanyProfileResponse>();
                    return result.Items ?? new List<CompanyProfileItem>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetCompanyProfiles");
            }
            return new List<CompanyProfileItem>();
        }
    
    }
}
