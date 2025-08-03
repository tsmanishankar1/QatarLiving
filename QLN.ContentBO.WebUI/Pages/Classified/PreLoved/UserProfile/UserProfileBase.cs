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

        protected List<BusinessVerificationItem> Listings { get; set; } = new();
        protected bool IsLoading { get; set; } = true;
        protected bool IsEmpty => !IsLoading && Listings.Count == 0;
        protected int TotalCount { get; set; }
        protected int currentPage { get; set; } = 1;
        protected int pageSize { get; set; } = 12;
        protected string SearchText { get; set; } = string.Empty;

        protected string SortIcon { get; set; } = Icons.Material.Filled.Sort;

        protected DateTime? dateCreated { get; set; }
        protected DateTime? datePublished { get; set; }

        protected DateTime? tempCreatedDate { get; set; }
        protected DateTime? tempPublishedDate { get; set; }

        protected bool showCreatedPopover { get; set; } = false;
        protected bool showPublishedPopover { get; set; } = false;
        protected string SelectedTab { get; set; } = ((int)CompanyStatus.Rejected).ToString();
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
                  
                    IsPromoted = isPromoted,
                    IsFeatured = isFeatured
                };


                var response = await ClassifiedService.GetPrelovedP2pListing(request);

                if (response?.IsSuccessStatusCode ?? false)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Raw Content: {content}");

                    var result = JsonSerializer.Deserialize<PagedResult<BusinessVerificationItem>>(content, new JsonSerializerOptions
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
                    Listings = result?.Items ?? new List<BusinessVerificationItem>();
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
        protected void OnSearchChanged(ChangeEventArgs e)
        {
            SearchText = e.Value?.ToString();
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
        protected async Task OnTabChanged(string newTab)
        {
            selectedTab = newTab;

            int? status = newTab switch
            {
                "verificationrequests" => 1,
                "rejected" => 2,
                "approved" => 3,
                _ => null
            };

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