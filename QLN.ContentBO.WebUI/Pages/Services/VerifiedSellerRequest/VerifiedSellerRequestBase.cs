using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Models;
namespace QLN.ContentBO.WebUI.Pages.Services.VerifiedSellerRequest
{
  public partial class VerifiedSellerRequestBase : ComponentBase
  {
    [Inject] public IServiceBOService _serviceBOService { get; set; }
    [Inject] ILogger<VerifiedSellerRequestBase> Logger { get; set; }
    protected PaginatedServiceResponse PaginatedData { get; set; } = new();
    public List<CompanyProfileItem> Listings { get; set; } = new();
    protected string selectedTab = "verificationrequests";
    protected List<ToggleTabs.TabOption> tabOptions = new()
        {
            new() { Label = "Verification Requests", Value = "verificationrequests" },
            new() { Label = "Rejected", Value = "rejected" },
            new() { Label = "Approved", Value = "approved" },
        };
    protected int currentPage = 1;
    protected async Task OnTabChanged(string newTab)
        {
            selectedTab = newTab;

            Status = newTab switch
            {
                "verificationrequests" => 1,
                "rejected" => 4,
                "approved" => 8,
                _ => null
            };
            Listings = await GetVerifiedSellerRequest();
            StateHasChanged();
        }

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
      Status = 1;
      Listings = await GetVerifiedSellerRequest();
    }

    protected async Task HandleSearch(string searchTerm)
    {
      // Search = searchTerm;
      // currentPage = 1;
      // PaginatedData = await LoadP2PListingsAsync();
      // StateHasChanged();
    }
    // protected async Task HandleDateFiltersChanged((DateTime? created, DateTime? published) filters)
    // {
    //   FromDate = filters.created;
    //   ToDate = filters.published;
    //   pageSize = 50;
    //   PaginatedData = await LoadP2PListingsAsync();
    //   StateHasChanged();
    // }
    // protected async Task HandleClearFilters()
    // {
    //   Search = string.Empty;
    //   SortBy = "asc";
    //   FromDate = null;
    //   ToDate = null;
    //   PublishedFrom = null;
    //   PublishedTo = null;
    //   currentPage = 1;
    //   PaginatedData = await LoadP2PListingsAsync();
    //   StateHasChanged();
    // }


    protected async Task HandleStatusChange(int? status)
    {
      currentPage = 1;
      pageSize = 12;
      Status = status;
      Listings = await GetVerifiedSellerRequest();
      StateHasChanged();
    }

    protected async Task HandlePageChange(int newPage)
    {
      currentPage = newPage;
      Listings = await GetVerifiedSellerRequest();
      StateHasChanged();
    }
    protected async Task HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            Listings = await GetVerifiedSellerRequest();
            StateHasChanged();
        }


    protected async Task HandleSort(bool sortOption)
    {
      // SortBy = sortOption ? "asc" : "desc";
      // PaginatedData = await LoadP2PListingsAsync();
      // StateHasChanged();
    }
    private async Task<List<CompanyProfileItem>> GetVerifiedSellerRequest()
    {
      try
      {
        var payload = new
        {
            isBasicProfile = false,
            vertical = 4,
            companyVerificationStatus = Status,
            pageNumber = currentPage,
            pageSize = pageSize
        };
        var response = await _serviceBOService.GetAllCompaniesAsync(payload);
        if (response.IsSuccessStatusCode)
        {
          var result = await response.Content.ReadFromJsonAsync<CompanyProfileResponse>();
          return result?.Items ?? new List<CompanyProfileItem>();
        }
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "GetVerifiedSellerRequest");
      }

      return new List<CompanyProfileItem>();
    }
  }
}