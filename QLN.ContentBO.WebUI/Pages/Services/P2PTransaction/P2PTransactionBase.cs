using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Services.P2PTransaction
{
  public partial class P2PTransactionBase : ComponentBase
  {
    [Inject] public IServiceBOService _serviceBOService { get; set; }
    [Inject] ILogger<P2PTransactionBase> Logger { get; set; }
    protected PaginatedP2PResponse PaginatedData { get; set; } = new();
    public List<ServiceP2PAdSummaryDto> Listings => PaginatedData.items;
    protected int currentPage = 1;
    protected int pageSize = 12;
    protected int? currentStatus = 1;
    public string? SortBy { get; set; }
    public string? Search { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DateTime? PublishedFrom { get; set; }
    public DateTime? PublishedTo { get; set; }
    protected override async Task OnInitializedAsync()
    {
      currentPage = 1;
      pageSize = 50;
      PaginatedData = await LoadP2PListingsAsync();
    }


    protected async Task HandleSearch(string searchTerm)
    {
      Search = searchTerm;
      currentPage = 1;
      PaginatedData = await LoadP2PListingsAsync();
      StateHasChanged();
    }

    protected async Task HandleSort(bool sortOption)
    {
      SortBy = sortOption ? "asc" : "desc";
      PaginatedData = await LoadP2PListingsAsync();
      StateHasChanged();
    }
    protected async Task HandlePageChange(int newPage)
    {
      currentPage = newPage;
      PaginatedData = await LoadP2PListingsAsync();
      StateHasChanged();
    }
    protected async Task HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            currentPage = 1;
            PaginatedData = await LoadP2PListingsAsync();
            StateHasChanged();
        }
        private async Task<PaginatedP2PResponse> LoadP2PListingsAsync()
    {
      try
      {
        var response = await _serviceBOService.GetPaginatedP2PTransactionListing(
            sortBy: SortBy,
            search: Search,
            fromDate: FromDate,
            toDate: ToDate,
            pageNumber: currentPage,
            pageSize: pageSize
        );

        if (response.IsSuccessStatusCode)
        {
          var result = await response.Content.ReadFromJsonAsync<PaginatedP2PResponse>();
          return result ?? new PaginatedP2PResponse();
        }
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "LoadP2PListingsAsync");
      }
      return new PaginatedP2PResponse();
    }

    }
}