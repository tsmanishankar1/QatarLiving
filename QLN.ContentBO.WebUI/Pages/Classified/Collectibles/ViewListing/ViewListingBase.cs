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
using QLN.ContentBO.WebUI.Components.AutoSelectDialog;
using MudBlazor;
using QLN.ContentBO.WebUI.Enums;

namespace QLN.ContentBO.WebUI.Pages.Classified.Collectibles.ViewListing
{
    public partial class ViewListingBase : ComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected NavigationManager NavManager { get; set; } = default!;
        [Inject] public IClassifiedService ClassifiedService { get; set; }
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
                "pendingApproval" => new() { { "status", (int)AdStatus.PendingApproval } },
                "published" => new() { { "status", (int)AdStatus.Published } },
                "unpublished" => new() { { "status", (int)AdStatus.Unpublished } },
                "p2p" => new() { { "adType", (int)AdType.P2P  } },
                "approved" => new() { { "status", (int)AdStatus.Approved } },
                "needChanges" => new() { { "status", (int)AdStatus.NeedsModification } },
                "removed" => new() { { "status", (int)AdStatus.Rejected  } },
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
                {
                    var createdUtc = DateTime.SpecifyKind(DateCreatedFilter.Value, DateTimeKind.Utc);
                    filters["createdAt"] = createdUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                }

                if (DatePublishedFilter.HasValue)
                {
                    var publishedUtc = DateTime.SpecifyKind(DatePublishedFilter.Value, DateTimeKind.Utc);
                    filters["publishedDate"] = publishedUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                }

                // Only include filters if any were added
                    if (filters.Any())
                    {
                        foreach (var kvp in filters)
                        {
                            payload[kvp.Key] = kvp.Value;
                        }
                    }


                // ✅ Log the actual payload
                // var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                // Logger.LogInformation("Classified Payload:\n{Payload}", payloadJson);

                var responses = await ClassifiedService.SearchClassifiedsViewListingAsync("getall-collectibles", payload);

                if (responses.Any() && responses[0].IsSuccessStatusCode)
                {
                    var result = await responses[0].Content.ReadFromJsonAsync<ClassifiedsCollectiblesApiResponse>();
                    if (result != null)
                    {
                        ClassifiedItems = result.ClassifiedsCollectibles;
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
          protected async Task HandleAddClicked()
        {
            var parameters = new DialogParameters
        {
            { "Title", "Create Ad" },
            { "Label", "User Email*" },
            { "ButtonText", "Continue" },
            { "ListItems", new List<DropdownItem>
                {
                    new() { Id = 1, Label = "john.doe@hotmail.com" },
                    new() { Id = 2, Label = "jane.doe@gmail.com" },
                    new() { Id = 3, Label = "alice@example.com" },
                    new() { Id = 4, Label = "bob@workmail.com" },
                    new() { Id = 5, Label = "emma@company.com" }
                }
            },
            { "OnSelect", EventCallback.Factory.Create<DropdownItem>(this, HandleSelect) }
        };

            DialogService.Show<AutoSelectDialog>("", parameters);
        }


        private Task HandleSelect(DropdownItem selected)
        {
            var targetUrl = $"/manage/classified/collectibles/createform?email={selected.Label}";
            NavManager.NavigateTo(targetUrl);
            return Task.CompletedTask;
        }
        
    }
}
