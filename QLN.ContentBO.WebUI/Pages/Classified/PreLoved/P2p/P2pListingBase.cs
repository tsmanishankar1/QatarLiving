using DocumentFormat.OpenXml.Wordprocessing;
using Google.Api;
using Markdig.Parsers;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;
using QLN.ContentBO.WebUI.Interfaces;

namespace QLN.ContentBO.WebUI.Pages.Classified.PreLoved.P2p
{
    public class P2pListingBase : QLComponentBase
    {

        [Inject]
        protected IClassifiedService ClassifiedService { get; set; } = default!;

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
        protected int currentPage { get; set; } = 1;
        protected int pageSize { get; set; } = 12;
        protected string SelectedTab { get; set; } = ((int)AdStatus.PendingApproval).ToString();

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        protected async Task LoadData()
        {
            IsLoading = true;
            StateHasChanged();

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
                var request = new FilterRequest
                {
                    PageNumber = currentPage,
                    PageSize = pageSize,
                    Status = status, 
                    SearchText = SearchText,
                    CreationDate = dateCreated,
                    PublishedDate = datePublished,
                    SortField = SortField,
                    SortDirection = SortDirection,
                    IsPromoted = isPromoted,
                    IsFeatured = isFeatured
                };

                
                var response = await ClassifiedService.GetPrelovedP2pListing(request);

                if (response?.IsSuccessStatusCode ?? false)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Raw Content: {content}");

                    var result = JsonSerializer.Deserialize<PagedResult<P2pListingModal>>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    if (result == null)
                    {
                        Console.WriteLine("Deserialized result is null");
                    }
                    else
                    {
                        Console.WriteLine($"Items Count: {result.Items?.Count ?? 0}, TotalCount: {result.TotalCount}");
                    }
                    Listings = result?.Items ?? new List<P2pListingModal>();
                    TotalCount = result?.TotalCount ?? 0;
                }
                else
                {
                    Console.WriteLine($"API call failed. StatusCode: {response?.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        protected async Task HandleTabChange(string newTab)
        {
            if (SelectedTab != newTab)
            {
                SelectedTab = newTab;
                currentPage = 1;
                await LoadData();
                StateHasChanged();
            }
        }

        protected async void OnSearchChanged(ChangeEventArgs e)
        {
            SearchText = e.Value?.ToString();
            currentPage = 1;
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
            pageSize = newSize;
            currentPage = 1;
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
