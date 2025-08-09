using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewStores
{
    public partial class ViewStoresBase : QLComponentBase
    {
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;

        [Inject] public IStoresService StoresService { get; set; } = default!;
        [Inject] public ILogger<ViewStoresBase> Logger { get; set; } = default!;
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected int TotalCount { get; set; }
        protected string SearchTerm { get; set; } = string.Empty;
        protected bool Ascending = true;
        protected List<CompanyProfileItem> StoreItems { get; set; } = [];

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var companyProfileResponse = await LoadStores();
                StoreItems = companyProfileResponse?.Items ?? new List<CompanyProfileItem>();
                TotalCount = companyProfileResponse?.TotalCount ?? 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync");
                throw;
            }
        }

        protected async Task HandleSearch(string searchTerm)
        {
            SearchTerm = searchTerm;

            var payload = new CompanyRequestPayload
            {
                IsBasicProfile = true,
                Status = 1,
                Vertical = 3,
                SubVertical = 3,
                Search = SearchTerm,
                SortBy = "",
                PageNumber = currentPage,
                PageSize = pageSize
            };

            var companyProfileResponse = await GetAllStoresListingAsync(payload);
            StoreItems = companyProfileResponse?.Items ?? [];
            TotalCount = companyProfileResponse?.TotalCount ?? 0;
        }

        protected async Task HandleSort(bool sortOption)
        {
            Ascending = sortOption;
            Console.WriteLine($"Sort triggered: {sortOption}");
            // Add logic to sort your listing data based on SortOption
        }

        protected async Task HandlePageChange(int newPage)
        {
            currentPage = newPage;
            
            var payload = new CompanyRequestPayload
            {
                IsBasicProfile = true,
                Status = 1,
                Vertical = 3,
                SubVertical = 3,
                Search = "",
                SortBy = "",
                PageNumber = currentPage,
                PageSize = pageSize
            };

            var companyProfileResponse = await GetAllStoresListingAsync(payload);
            StoreItems = companyProfileResponse?.Items ?? [];
            TotalCount = companyProfileResponse?.TotalCount ?? 0;
        }

        protected async Task HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            currentPage = 1; // reset to first page
            
            var payload = new CompanyRequestPayload
            {
                IsBasicProfile = true,
                Status = 1,
                Vertical = 3,
                SubVertical = 3,
                Search = "",
                SortBy = "",
                PageNumber = currentPage,
                PageSize = pageSize
            };

            var companyProfileResponse = await GetAllStoresListingAsync(payload);
            StoreItems = companyProfileResponse?.Items ?? [];
            TotalCount = companyProfileResponse?.TotalCount ?? 0;
        }

        protected void OnViewClicked(CompanyProfileItem store)
        {
            var name = "Rashid";
            // NavigationManager.NavigateTo($"/manage/classified/stores/createform/{name}");

        }

        private async Task<CompanyProfileResponse> LoadStores()
        {
            try
            {
                var payload = new CompanyRequestPayload
                {
                    IsBasicProfile = true,
                    Status = 1,
                    Vertical = 3,
                    SubVertical = 3,
                    Search = "",
                    SortBy = "",
                    PageNumber = currentPage,
                    PageSize = pageSize
                };

                return await GetAllStoresListingAsync(payload);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadStores");
                return new();
            }
        }

        private async Task<CompanyProfileResponse> GetAllStoresListingAsync(CompanyRequestPayload companyRequestPayload)
        {
            try
            {
                var apiResponse = await StoresService.GetAllStoresListing(companyRequestPayload);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var companyProfileResponse = await apiResponse.Content.ReadFromJsonAsync<CompanyProfileResponse>();

                    return companyProfileResponse ?? new();
                }

                return new();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetAllStoresListingAsync");
                return new();
            }
        }
    }
}
