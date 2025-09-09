using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Services.ViewSubscriptionListing
{
  public partial class ViewSubscriptionListingBase : ComponentBase
  {
    [Inject] public IServiceBOService _serviceBOService { get; set; }
    [Inject] ILogger<ViewSubscriptionListingBase> Logger { get; set; }
    protected PaginatedPaymentSummaryResponse PaginatedData { get; set; } = new();
    public List<ServiceAdPaymentSummaryDto> Listings => PaginatedData.items;
    [Inject] IServiceBOService serviceBOService { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    protected int currentPage = 1;
    protected int pageSize = 12;
    protected int? currentStatus = 1;
    public string? SortBy { get; set; }
    public string? SelectedSubscriptionType { get; set; }
    public string? Search { get; set; }
    protected override async Task OnInitializedAsync()
    {
      currentPage = 1;
      pageSize = 12;
      PaginatedData = await LoadSubscriptionListingsAsync();
    }


    protected async Task HandleSearch(string searchTerm)
    {
      Search = searchTerm;
      currentPage = 1;
      PaginatedData = await LoadSubscriptionListingsAsync();
      StateHasChanged();
    }
    protected async Task HandleDateFiltersChanged((DateTime? startDate, DateTime? endDate) dates)
    {
      FromDate = dates.startDate;
      ToDate = dates.endDate;
      PaginatedData = await LoadSubscriptionListingsAsync();
      StateHasChanged();
    }

    protected async Task HandleSort(bool sortOption)
    {
      SortBy = sortOption ? "asc" : "desc";
      PaginatedData = await LoadSubscriptionListingsAsync();
      StateHasChanged();
    }
    protected async Task HandleSubscriptionType(string subscriptionType)
    {
      SelectedSubscriptionType = subscriptionType;
      PaginatedData = await LoadSubscriptionListingsAsync();
      StateHasChanged();
    }
    protected async Task HandlePageChange(int newPage)
    {
      currentPage = newPage;
      PaginatedData = await LoadSubscriptionListingsAsync();
      StateHasChanged();
    }
    protected async Task HandlePageSizeChange(int newPageSize)
    {
      pageSize = newPageSize;
      currentPage = 1;
      PaginatedData = await LoadSubscriptionListingsAsync();
      StateHasChanged();
    }
    protected async Task HandleClearFilters()
    {
      Search = null;
      SortBy = "asc";
      FromDate = null;
      ToDate = null;
      currentPage = 1;
      pageSize = 12;
      PaginatedData = await LoadSubscriptionListingsAsync();
      StateHasChanged();
    }

    protected async Task<PaginatedPaymentSummaryResponse> LoadSubscriptionListingsAsync()
    {
      try
      {
        var response = await _serviceBOService.GetPaginatedSubscriptionListing(
            sortBy: SortBy,
            search: Search,
            fromDate: FromDate,
            toDate: ToDate,
            pageNumber: currentPage,
            pageSize: pageSize,
            subscriptionType: SelectedSubscriptionType
        );
        if (response.IsSuccessStatusCode)
        {
          var result = await response.Content.ReadFromJsonAsync<PaginatedPaymentSummaryResponse>();
          return result ?? new PaginatedPaymentSummaryResponse();
        }
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "LoadSubscriptionListingsAsync");
      }
      return new PaginatedPaymentSummaryResponse();
    }
    protected async Task ReloadLoadSubscriptionListingsAsync()
    {
      try
      {
        var response = await _serviceBOService.GetPaginatedSubscriptionListing(
            sortBy: SortBy,
            search: Search,
            fromDate: FromDate,
            toDate: ToDate,
            pageNumber: currentPage,
            pageSize: pageSize,
            subscriptionType: SelectedSubscriptionType
        );
        if (response.IsSuccessStatusCode)
        {
          var result = await response.Content.ReadFromJsonAsync<PaginatedPaymentSummaryResponse>();
          PaginatedData = result ?? new PaginatedPaymentSummaryResponse();
          PaginatedData.items = result.items;
        }
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "LoadSubscriptionListingsAsync");
      }
    }
       


    }
}