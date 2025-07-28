using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.ViewListing
{
    public partial class ViewListingBase : ComponentBase
    {
        [Inject]
        public IClassifiedService ClassifiedService { get; set; }
        [Inject] private ILogger<ViewListingBase> Logger { get; set; } = default!;
        protected string SearchTerm { get; set; } = string.Empty;
        protected bool Ascending { get; set; } = true;
        protected List<ClassifiedItemViewListing> ClassifiedItems { get; set; } = new();
        protected int TotalCount { get; set; }
        private DateTime? DateCreatedFilter { get; set; }
        private DateTime? DatePublishedFilter { get; set; }
        protected string selectedTab = "pendingApproval";
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 12;
        protected bool IsLoading = true;

        private Dictionary<string, object> GetFiltersForTab()
        {
            return selectedTab switch
            {
                "featured" => new() { { "isFeatured", true } },
                "promoted" => new() { { "isPromoted", true } },
                "pendingApproval" => new() { { "status", "PendingApproval" } },
                "published" => new() { { "status", "Published" } },
                "unpublished" => new() { { "status", "Unpublished" } },
                "p2p" => new() { { "adType", "P2P" } },
                _ => new()
            };
        }

        protected async Task HandleTabChange(string newTab)
        {
            selectedTab = newTab;
            await LoadClassifiedsAsync();
        }
        protected async Task HandlePageChanged(int newPage)
        {
            CurrentPage = newPage;
            await LoadClassifiedsAsync();
        }

        protected async Task HandlePageSizeChanged(int newSize)
        {
            PageSize = newSize;
            CurrentPage = 1;
            await LoadClassifiedsAsync();
        }
        protected async Task HandleDateFiltersChanged((DateTime? created, DateTime? published) filters)
        {
            DateCreatedFilter = filters.created;
            DatePublishedFilter = filters.published;
            await LoadClassifiedsAsync();
        }

        protected async Task HandleClearFilters()
        {
            SearchTerm = string.Empty;
            Ascending = true;
            DateCreatedFilter = null;
            DatePublishedFilter = null;
            CurrentPage = 1;
            await LoadClassifiedsAsync();
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadClassifiedsAsync();
        }

        protected async Task HandleSearch(string searchTerm)
        {
            SearchTerm = searchTerm;
            await LoadClassifiedsAsync();
        }

        protected async Task HandleSort(bool sortAscending)
        {
            Ascending = sortAscending;
            await LoadClassifiedsAsync();
        }

          private async Task LoadClassifiedsAsync()
        {
            try
            {
                IsLoading = true;
                var payload = new Dictionary<string, object>
                {
                    ["text"] = SearchTerm,
                    ["orderBy"] = Ascending ? "createdAt desc" : "createdAt asc",
                    ["pageNumber"] = CurrentPage,
                    ["pageSize"] = PageSize
                };

                 // Declare filters regardless of selectedTab
                var filters = GetFiltersForTab();

              if (DateCreatedFilter.HasValue)
                filters["createdAt"] = DateCreatedFilter.Value.ToString("yyyy-MM-dd");

              if (DatePublishedFilter.HasValue)
                filters["publishedDate"] = DatePublishedFilter.Value.ToString("yyyy-MM-dd");


                //  Only add if there's at least one filter
                if (filters.Any())
                    payload["filters"] = filters;

                // ✅ Log the actual payload
                // var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                // Logger.LogInformation("Classified Payload:\n{Payload}", payloadJson);

                var responses = await ClassifiedService.SearchClassifiedsViewListingAsync("getall-items", payload);

                if (responses.Any() && responses[0].IsSuccessStatusCode)
                {
                    var result = await responses[0].Content.ReadFromJsonAsync<ClassifiedsApiResponse>();
                    if (result != null)
                    {
                        ClassifiedItems = result.ClassifiedsItems;
                        TotalCount = result.TotalCount;
                        return;
                    }
                }

                // Fallback if no results or error
                ClassifiedItems = new List<ClassifiedItemViewListing>();
                TotalCount = 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading classifieds.");
                ClassifiedItems = new List<ClassifiedItemViewListing>();
                TotalCount = 0;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
