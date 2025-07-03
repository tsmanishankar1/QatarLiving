using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Components.BreadCrumb;
using QLN.Common.DTO_s;
using Microsoft.JSInterop;

namespace QLN.Web.Shared.Pages.Classifieds.Items.Components
{
    public class ItemListSectionBase : ComponentBase
    {
        [Inject] protected SearchStateService SearchState { get; set; } = default!;
        [Parameter] public string ViewMode { get; set; }
        [Parameter] public bool Loading { get; set; } = false;
        [Parameter] public bool IsSearchPerformed { get; set; }
        [Parameter] public EventCallback<string> OnSearch { get; set; }
       [Inject] NavigationManager NavigationManager { get; set; }
       [Parameter] public List<ClassifiedsIndex> Items { get; set; } = new();
       [Parameter]
       public int TotalCount { get; set; } = 0;

        protected List<BreadcrumbItem> breadcrumbItems = new();
        protected string selectedSort = "default";
        protected int currentPage => SearchState.ItemCurrentPage;
        protected int pageSize => SearchState.ItemPageSize;

       protected async void HandlePageChange(int newPage)
        {
            SearchState.ItemSetPage(newPage);
            if (OnSearch.HasDelegate)
                await OnSearch.InvokeAsync(SearchState.ItemSearchText ?? string.Empty);
        }

        protected async void HandlePageSizeChange(int newSize)
        {
            SearchState.ItemSetPageSize(newSize);
            if (OnSearch.HasDelegate)
                await OnSearch.InvokeAsync(SearchState.ItemSearchText ?? string.Empty);
    }
    protected List<object> GetPageWithAd(List<ClassifiedsIndex> items, int currentPage)
    {
        var result = new List<object>();
        int adIndex = GetAdInsertIndex(currentPage);

        for (int i = 0; i < items.Count; i++)
        {
            if (i == adIndex)
            {
                result.Add("ad"); // use string or special marker for ad
            }

            result.Add(items[i]);
        }

        // if ad wasn't inserted (e.g., adIndex > items.Count), append it
        if (!result.Contains("ad"))
        {
            result.Add("ad");
        }

        // Trim if somehow more than 12 items
        return result.Take(12).ToList();
    }

    private int GetAdInsertIndex(int page)
    {
        // Always start from 3, vary based on page number
        var positions = new[] { 2, 4, 9, 5, 6 };
        return positions[(page - 1) % positions.Length];
    }

       protected void OnClickCardItem(ClassifiedsIndex item)
        {
            NavigationManager.NavigateTo($"/qln/classifieds/items/details/{item.Id}");
        }
          protected int WindowWidth { get; set; }

        [Inject] protected IJSRuntime JS { get; set; } = default!;
        private DotNetObjectReference<ItemListSectionBase>? _objectRef;

        [JSInvokable]
        public void UpdateWindowWidth(int width)
        {
            WindowWidth = width;
            StateHasChanged();
           if (WindowWidth <= 992 && ViewMode != "grid")
            {
                ViewMode = "grid";
                StateHasChanged();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _objectRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("blazorResize.registerResizeCallback", _objectRef);
            }
        }

        public void Dispose()
        {
            _objectRef?.Dispose();
        }

        protected async Task ClearSearch()
        {
            SearchState.ItemSearchText = null;
            SearchState.ItemCategory = null;
            SearchState.ItemBrand = null;
            SearchState.ItemMinPrice = null;
            SearchState.ItemMaxPrice = null;
            SearchState.ItemViewMode ??= "grid";
            SearchState.ItemSubCategory = null;
            SearchState.ItemSubSubCategory = null;
            SearchState.ItemFieldFilters.Clear();
            SearchState.ItemHasWarrantyCertificate = false;
            SearchState.ItemSetPage(1);
            if (OnSearch.HasDelegate)
            {
                await OnSearch.InvokeAsync("");
            }
        }

        protected override void OnInitialized()
        {
            breadcrumbItems = new()
            {
                new() { Label = "Classifieds", Url = "/qln/classifieds" },
                new() { Label = "Items", Url = "/qln/classifieds/items", IsLast = true }
            };
        }
        protected async Task OnSortChanged(string newSortId)
        {
            selectedSort = newSortId;
            SearchState.ItemSetPage(1);

            var selectedOption = sortOptions.FirstOrDefault(x => x.Id == newSortId);
            SearchState.ItemSortBy = selectedOption?.OrderByValue;

            if (OnSearch.HasDelegate)
            {
                await OnSearch.InvokeAsync(SearchState.ItemSearchText ?? string.Empty);
            }
        }


    public class SortOption
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string? OrderByValue { get; set; } 
    }

        protected List<SortOption> sortOptions = new()
        {
            new() { Id = "default", Label = "Default", OrderByValue = null },
            new() { Id = "priceLow", Label = "Price: Low to High", OrderByValue = "price asc" },
            new() { Id = "priceHigh", Label = "Price: High to Low", OrderByValue = "price desc" }
        };


    }
}
