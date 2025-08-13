using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;
using QLN.ContentBO.WebUI.Interfaces;
using Nextended.Core.Extensions;

namespace QLN.ContentBO.WebUI.Pages.Classified.PreLoved.P2p
{
    public class P2pListingBase : QLComponentBase
    {

        [Inject] protected IPrelovedService PrelovedService { get; set; } = default!;
        [Inject] protected ILogger<P2pListingBase> Logger { get; set; } = default!;
        protected List<P2pListingModal> Listings { get; set; } = new();
        protected string SearchText { get; set; } = string.Empty;
        protected string SortIcon { get; set; } = Icons.Material.Filled.Sort;
        protected string SortDirection { get; set; } = "asc";
        protected string SortField { get; set; } = "creationDate";

        protected DateTime? dateCreated { get; set; }
        protected DateTime? datePublished { get; set; }
        protected DateTime? tempCreatedDate { get; set; }
        protected DateTime? tempPublishedDate { get; set; }

        protected bool showCreatedPopover { get; set; } = false;
        protected bool showPublishedPopover { get; set; } = false;

        protected bool IsLoading { get; set; } = true;
        protected bool IsEmpty => !IsLoading && Listings.Count == 0;
        protected int TotalCount { get; set; }
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 12;
        protected string SelectedTab { get; set; } = AdStatusEnum.PendingApproval.ToString();

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        protected async Task LoadData()
        {
            IsLoading = true;

            try
            {
                int? status = null;
                bool? isPromoted = null;
                bool? isFeatured = null;

                if (int.TryParse(SelectedTab, out var tabValue))
                {
                    status = tabValue;
                }
                else
                {
                    if (SelectedTab == "promoted")
                        isPromoted = true;
                    else if (SelectedTab == "featured")
                        isFeatured = true;
                }

                var request = new PrelovedP2PSubscriptionQuery
                {
                    Status = SelectedTab.Capitalize(),
                    CreatedDate = dateCreated.ToString(),
                    PublishedDate = datePublished.ToString(),
                    Page = CurrentPage,
                    PageSize = PageSize,
                    Search = SearchText,
                    SortBy = SortField,
                    SortOrder = SortDirection
                };

                var response = await PrelovedService.GetPrelovedP2pListing(request);

                if (response?.IsSuccessStatusCode ?? false)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<PagedResult<P2pListingModal>>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    Listings = result?.Items ?? new List<P2pListingModal>();
                    TotalCount = result?.TotalCount ?? 0;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadData");
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task HandleTabChange(string newTab)
        {
            if (SelectedTab != newTab)
            {
                SelectedTab = newTab;
                CurrentPage = 1;
                await LoadData();
                StateHasChanged();
            }
        }

        protected async void OnSearchChanged(ChangeEventArgs e)
        {
            SearchText = e.Value?.ToString();
            CurrentPage = 1;
            await LoadData();
        }

        protected async void ToggleSort()
        {
            SortDirection = SortDirection == "asc" ? "desc" : "asc";
            SortIcon = SortDirection == "asc"
                ? Icons.Material.Filled.ArrowUpward
                : Icons.Material.Filled.ArrowDownward;
            await LoadData();
        }


        protected async Task HandlePageChanged(int newPage)
        {
            await LoadData();
        }

        protected async Task HandlePageSizeChanged(int newSize)
        {
            PageSize = newSize;
            CurrentPage = 1;
            await LoadData();
        }
        protected void ToggleCreatedPopover()
        {
            showCreatedPopover = !showCreatedPopover;
        }

        protected void CancelCreatedPopover()
        {
            tempCreatedDate = dateCreated;
            showCreatedPopover = false;
        }

        protected void ConfirmCreatedPopover()
        {
            dateCreated = tempCreatedDate;
            showCreatedPopover = false;
        }

        protected void TogglePublishedPopover()
        {
            showPublishedPopover = !showPublishedPopover;
        }

        protected void CancelPublishedPopover()
        {
            tempPublishedDate = datePublished;
            showPublishedPopover = false;
        }

        protected void ConfirmPublishedPopover()
        {
            datePublished = tempPublishedDate;
            showPublishedPopover = false;
        }

        protected void ClearFilters()
        {
            dateCreated = null;
            datePublished = null;
            SearchText = string.Empty;
        }
    }
}
