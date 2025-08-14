using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.AutoSelectDialog;
using QLN.ContentBO.WebUI.Enums;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Classified.Collectibles.ViewListing
{
    public partial class ViewListingBase : QLComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] public ICollectiblesService CollectiblesService { get; set; }
        [Inject] private ILogger<ViewListingBase> Logger { get; set; } = default!;
        protected string? SearchTerm { get; set; }
        protected bool Ascending { get; set; } = true;
        protected List<CollectibleItem> ClassifiedItems { get; set; } = [];
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
                "p2p" => new() { { "adType", (int)AdType.P2P } },
                "needChanges" => new() { { "status", (int)AdStatus.NeedsModification } },
                "removed" => new() { { "status", (int)AdStatus.Rejected } },
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
                var request = new ItemsRequest
                {
                    Text = SearchTerm,
                    OrderBy = Ascending ? "createdAt desc" : "createdAt asc",
                    PageNumber = CurrentPage,
                    PageSize = PageSize
                };

                var filters = GetFiltersForTab();

                if (DateCreatedFilter.HasValue)
                {
                    request.CreatedAt = DateTime.SpecifyKind(DateCreatedFilter.Value, DateTimeKind.Utc);
                }

                if (DatePublishedFilter.HasValue)
                {
                    request.PublishedDate = DateTime.SpecifyKind(DatePublishedFilter.Value, DateTimeKind.Utc);
                }

                if (filters.TryGetValue("status", out var statusValue) && int.TryParse(statusValue.ToString(), out var status))
                {
                    request.Status = status;
                }

                if (filters.TryGetValue("adType", out var adTypeValue) && int.TryParse(adTypeValue.ToString(), out var adType))
                {
                    request.AdType = adType;
                }

                if (filters.TryGetValue("isFeatured", out var isFeaturedValue) && bool.TryParse(isFeaturedValue.ToString(), out var isFeatured))
                {
                    request.IsFeatured = isFeatured;
                }

                if (filters.TryGetValue("isPromoted", out var isPromotedValue) && bool.TryParse(isPromotedValue.ToString(), out var isPromoted))
                {
                    request.IsPromoted = isPromoted;
                }

                var response = await CollectiblesService.GetAllListing(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CollectibleResponse>();
                    if (result != null)
                    {
                        ClassifiedItems = result.ClassifiedsCollectibles;
                        TotalCount = result.TotalCount;
                        return;
                    }
                }

                // Fallback if no results or error
                ClassifiedItems = [];
                TotalCount = 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading classifieds.");
                ClassifiedItems = [];
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
                { "OnSelect", EventCallback.Factory.Create<DropdownItem>(this, HandleSelect) }
            };

            await DialogService.ShowAsync<AutoSelectDialog>("", parameters);
        }

        private Task HandleSelect(DropdownItem selected)
        {
            if (selected == null || string.IsNullOrWhiteSpace(selected.Label))
            {
                return Task.CompletedTask;
            }
            var targetUrl = $"/manage/classified/collectibles/createform?email={selected.Label}&uid={selected.Id}";
            NavManager.NavigateTo(targetUrl, true);
            return Task.CompletedTask;
        }
    }
}
