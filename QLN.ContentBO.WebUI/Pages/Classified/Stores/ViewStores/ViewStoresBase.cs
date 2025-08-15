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
        protected string? SearchTerm { get; set; } = null;
        protected string? SortBy { get; set; } = null;
        protected string? SubscriptionType { get; set; } = null;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        protected bool Ascending = true;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        protected List<CompanySubscriptionDto> StoreItems { get; set; } = [];

        protected override async Task OnInitializedAsync()
        {
            try
            {
                 await LoadStores();
                StateHasChanged();
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

            var payload = new CompanySubscriptionFilter
            {
                SubscriptionType = SubscriptionType,
                StartDate = StartDate,
                EndDate = EndDate,
                SearchTerm = searchTerm,
                SortBy = Ascending ? "asc" : "desc",
                PageNumber = currentPage,
                PageSize = pageSize
            };
            var companyProfileResponse = await GetAllStoresListingAsync(payload);
            StoreItems = companyProfileResponse?.Records ?? [];
            TotalCount = companyProfileResponse?.TotalRecords ?? 0;
        }
        protected async Task HandleTypeChange(string type)
        {
            SubscriptionType = type;
            var payload = new CompanySubscriptionFilter
            {
                SubscriptionType = type,
                StartDate = StartDate,
                EndDate = EndDate,
                SearchTerm = SearchTerm,
                SortBy = Ascending ? "asc" : "desc",
                PageNumber = currentPage,
                PageSize = pageSize
            };
            var companyProfileResponse = await GetAllStoresListingAsync(payload);
            StoreItems = companyProfileResponse?.Records ?? [];
            TotalCount = companyProfileResponse?.TotalRecords ?? 0;
        }

        protected async Task HandleSort(bool sortOption)
        {
            Ascending = sortOption;
            var payload = new CompanySubscriptionFilter
            {
                SubscriptionType = SubscriptionType,
                StartDate = StartDate,
                EndDate = EndDate,
                SearchTerm = SearchTerm,
                SortBy = sortOption ? "asc" : "desc",
                PageNumber = currentPage,
                PageSize = pageSize
            };
            var companyProfileResponse = await GetAllStoresListingAsync(payload);
            StoreItems = companyProfileResponse?.Records ?? [];
            TotalCount = companyProfileResponse?.TotalRecords ?? 0;
        }
        protected async Task HandleDateFiltersChanged((DateTime? createdFrom, DateTime? createdTo) filters)
        {
            StartDate = filters.createdFrom;
            EndDate = filters.createdTo;
             var payload = new CompanySubscriptionFilter
            {
                SubscriptionType = SubscriptionType,
                StartDate = filters.createdFrom,
                EndDate = filters.createdTo,
                SearchTerm = SearchTerm,
                SortBy = Ascending ? "asc" : "desc",
                PageNumber = currentPage,
                PageSize = pageSize
            };
            var companyProfileResponse = await GetAllStoresListingAsync(payload);
            StoreItems = companyProfileResponse?.Records ?? [];
            TotalCount = companyProfileResponse?.TotalRecords ?? 0;
        }



        protected async Task HandlePageChange(int newPage)
        {
            currentPage = newPage;
           var payload = new CompanySubscriptionFilter
            {
                SubscriptionType = SubscriptionType,
                StartDate = StartDate,
                EndDate = EndDate,
                SearchTerm = SearchTerm,
                SortBy = Ascending ? "asc" : "desc",
                PageNumber = newPage,
                PageSize = pageSize
            };
            var companyProfileResponse = await GetAllStoresListingAsync(payload);
            StoreItems = companyProfileResponse?.Records ?? [];
            TotalCount = companyProfileResponse?.TotalRecords ?? 0;
        }

        protected async Task HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            
             var payload = new CompanySubscriptionFilter
            {
                 SubscriptionType = SubscriptionType,
                StartDate = StartDate,
                EndDate = EndDate,
                SearchTerm = SearchTerm,
                SortBy = Ascending ? "asc" : "desc",
                PageNumber = currentPage,
                PageSize = pageSize
            };
            var companyProfileResponse = await GetAllStoresListingAsync(payload);
            StoreItems = companyProfileResponse?.Records ?? [];
            TotalCount = companyProfileResponse?.TotalRecords ?? 0;
        }

        protected void OnViewClicked(CompanySubscriptionDto store)
        {
            var name = "Rashid";
            // NavigationManager.NavigateTo($"/manage/classified/stores/createform/{name}");

        }

        private async Task LoadStores()
        {
            try
            {
                var payload = new CompanySubscriptionFilter
                {
                    SubscriptionType = SubscriptionType,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    SearchTerm = SearchTerm,
                    SortBy = SortBy,
                    PageNumber = currentPage,
                    PageSize = pageSize
                };
                var companyProfileResponse = await GetAllStoresListingAsync(payload);
                StoreItems = companyProfileResponse?.Records ?? new List<CompanySubscriptionDto>();
                TotalCount = companyProfileResponse?.TotalRecords ?? 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadStores");
            }
        }

        private async Task<CompanyStoresResponse> GetAllStoresListingAsync(CompanySubscriptionFilter companyRequestPayload)
        {
            try
            {
                var apiResponse = await StoresService.GetAllStoresListing(companyRequestPayload);

                if (apiResponse.IsSuccessStatusCode)
                {
                    var companyProfileResponse = await apiResponse.Content.ReadFromJsonAsync<CompanyStoresResponse>();

                    if (companyProfileResponse != null)
                    {
                        Logger.LogInformation("Parsing completed successfully.");
                    }
                    else
                    {
                        Logger.LogWarning("Parsing returned null.");
                    }

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
