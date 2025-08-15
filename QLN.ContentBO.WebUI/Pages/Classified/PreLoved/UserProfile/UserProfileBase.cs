using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Parsers;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Classified.PreLoved.UserProfile
{
    public class UserProfileBase : QLComponentBase
    {
        [Inject]
        protected IClassifiedService ClassifiedService { get; set; } = default!;
         [Inject] IServiceBOService serviceBOService { get; set; }
        protected List<CompanyProfileItem> Listings { get; set; } = new();
        protected bool IsLoading { get; set; } = true;
        protected bool IsEmpty => !IsLoading && Listings.Count == 0;
        protected int TotalCount { get; set; }
        protected int currentPage { get; set; } = 1;
        protected int pageSize { get; set; } = 12;
        public string? SortBy { get; set; }
        protected string? SearchText { get; set; } = string.Empty;
        public int? Status { get; set; }
        protected string SortIcon { get; set; } = Icons.Material.Filled.Sort;
        protected string SelectedTab { get; set; } = ((int)CompanyStatus.Rejected).ToString();
        protected override async Task OnInitializedAsync()
        {
            currentPage = 1;
            pageSize = 12;
            Status = 1;
            Listings = await GetCompanyProfiles();
        }
        protected async Task OnSearchChanged(ChangeEventArgs e)
        {
            SearchText = e.Value?.ToString();
            Listings = await GetCompanyProfiles();
        }
        protected async Task HandleSort(bool sortOption)
        {
            SortBy = sortOption ? "asc" : "desc";
            Listings = await GetCompanyProfiles();
            StateHasChanged();
        }
        protected void ToggleSort()
        {
            SortIcon = SortIcon == Icons.Material.Filled.ArrowDownward
                ? Icons.Material.Filled.ArrowUpward
                : Icons.Material.Filled.ArrowDownward;

        }
        protected string selectedTab = "verificationrequests";
        protected List<ToggleTabs.TabOption> tabOptions = new()
        {
            new() { Label = "Verification Requests", Value = "verificationrequests" },
            new() { Label = "Rejected", Value = "rejected" },
            new() { Label = "Approved", Value = "approved" },
        };

        protected async Task HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            Listings = await GetCompanyProfiles();
            StateHasChanged();
        }
        protected async Task HandlePageChange(int newPage)
        {
            currentPage = newPage;
            Listings = await GetCompanyProfiles();
            StateHasChanged();
        }
        protected async Task HandleStatusChange(int? status)
        {
            Status = status;
            Listings = await GetCompanyProfiles();
            StateHasChanged();
        }
        protected async Task OnTabChanged(string newTab)
        {
            selectedTab = newTab;

            Status = newTab switch
            {
                "verificationrequests" => 1,
                "rejected" => 2,
                "approved" => 3,
                _ => null
            };
            HandleStatusChange(Status);
            StateHasChanged();
        }
        protected void ClearFilters()
        {
            SearchText = null;
            Status = null;
            SortBy = null;
        }
        private async Task<List<CompanyProfileItem>> GetCompanyProfiles()
        {
            try
            {
                IsLoading = true;
                var payload = new
                {
                    vertical = Vertical.Classifieds,
                    subVertical = SubVertical.Preloved,
                    companyVerificationStatus = Status,
                    search = SearchText,
                    sortBy = SortBy,
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
            finally
            {
                IsLoading = false;
            }

            return new List<CompanyProfileItem>();
        }

    }
}