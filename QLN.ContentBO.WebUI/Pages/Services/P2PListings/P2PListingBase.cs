using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;
namespace QLN.ContentBO.WebUI.Pages.Services.P2PListings
{
  public partial class P2PListingBase : ComponentBase
  {
    [Inject] public IServiceBOService _serviceBOService { get; set; }
     [Inject] public IDialogService DialogService { get; set; }
    [Inject] ILogger<P2PListingBase> Logger { get; set; }
    protected PaginatedServiceResponse PaginatedData { get; set; } = new();
    public List<ServiceAdSummaryDto> Listings => PaginatedData.items;
    [Inject] ISnackbar Snackbar { get; set; }
     [Parameter] public ItemEditAdPost AdModel { get; set; } = new();
    protected int currentPage = 1;
    protected int pageSize = 12;
    protected int? currentStatus = 1;
    public string? SortBy { get; set; }
    public string? Search { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DateTime? PublishedFrom { get; set; }
    public DateTime? PublishedTo { get; set; }
    public int? Status { get; set; }
    public bool? IsPromoted { get; set; }
    public bool? IsFeatured { get; set; }
    protected override async Task OnInitializedAsync()
    {
      currentPage = 1;
      pageSize = 12;
      Status = 2;
      PaginatedData = await LoadP2PListingsAsync();
    }

    protected async Task HandleSearch(string searchTerm)
    {
      Search = searchTerm;
      currentPage = 1;
      PaginatedData = await LoadP2PListingsAsync();
      StateHasChanged();
    }
    protected async Task HandleDateFiltersChanged((DateTime? createdFrom,DateTime? createdTo ,DateTime? publishedFrom,DateTime? publishedTo) filters)
    {
      FromDate = filters.createdFrom;
      ToDate = filters.createdTo;
      PublishedFrom = filters.publishedFrom;
      PublishedTo = filters.publishedTo;
      PaginatedData = await LoadP2PListingsAsync();
      StateHasChanged();
    }
    protected async Task HandleClearFilters()
    {
      Search = string.Empty;
      SortBy = "asc";
      FromDate = null;
      ToDate = null;
      PublishedFrom = null;
      PublishedTo = null;
      currentPage = 1;
      PaginatedData = await LoadP2PListingsAsync();
      StateHasChanged();
    }
    protected async Task HandleStatusChange(int? status)
    {
      currentPage = 1;
      Status = status;
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


    protected async Task HandleSort(bool sortOption)
    {
      SortBy = sortOption ? "asc" : "desc";
      PaginatedData = await LoadP2PListingsAsync();
      StateHasChanged();
    }
    private async Task<PaginatedServiceResponse> LoadP2PListingsAsync()
    {
      try
      {
        var response = await _serviceBOService.GetPaginatedP2PListing(
            sortBy: SortBy,
            search: Search,
            fromDate: FromDate,
            toDate: ToDate,
            publishedFrom: PublishedFrom,
            publishedTo: PublishedTo,
            status: Status,
            isPromoted: IsPromoted,
            isFeatured: IsFeatured,
            pageNumber: currentPage,
            pageSize: pageSize
        );
        if (response.IsSuccessStatusCode)
        {
          var result = await response.Content.ReadFromJsonAsync<PaginatedServiceResponse>();
          return result ?? new PaginatedServiceResponse();
        }
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "LoadP2PListingsAsync");
      }
      return new PaginatedServiceResponse();
    }
  }
}