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
            Console.WriteLine($"Search triggered: {SearchTerm}");
            // Add logic to filter your listing data based on SearchTerm
        }

        protected async Task HandleSort(bool sortOption)
        {
            Ascending = sortOption;
            Console.WriteLine($"Sort triggered: {sortOption}");
            // Add logic to sort your listing data based on SortOption
        }

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
                    PageNumber = 1,
                    PageSize = 12
                };

                var apiResponse = await StoresService.GetAllStoresListing(payload);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var companyProfileResponse = await apiResponse.Content.ReadFromJsonAsync<CompanyProfileResponse>();

                    return companyProfileResponse ?? new();
                }

                return new();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadStores");
                return new();
            }
        }
    }
}
