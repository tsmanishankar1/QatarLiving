using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Components.BreadCrumb;
using QLN.Common.DTO_s;
using Microsoft.JSInterop;

namespace QLN.Web.Shared.Pages.Classifieds.Preloved.Components
{
    public class PrelovedListSectionBase : ComponentBase
    {
        [Inject] protected SearchStateService SearchState { get; set; } = default!;
        [Parameter] public string ViewMode { get; set; }
        [Parameter] public bool Loading { get; set; } = false;
        [Parameter] public bool IsSearchPerformed { get; set; }
        [Parameter] public EventCallback<string> OnSearch { get; set; }
       [Inject] NavigationManager NavigationManager { get; set; }
       [Parameter] public List<ClassifiedsIndex> Items { get; set; } = new();

        protected IEnumerable<ClassifiedsIndex> PagedItems =>
            Items.Skip((currentPage - 1) * pageSize).Take(pageSize);


        protected List<BreadcrumbItem> breadcrumbItems = new();
        protected string selectedSort = "default";
        protected int currentPage = 1;
        protected int pageSize = 12;

        protected void HandlePageChange(int newPage)
        {
            currentPage = newPage;
            StateHasChanged();
        }

        protected void HandlePageSizeChange(int newSize)
        {
            pageSize = newSize;
            currentPage = 1;
            StateHasChanged();
        }

        protected bool IsFirstPage => currentPage == 1;
        protected bool IsLastPage => currentPage * pageSize >= Items.Count;

        protected void OnFilterChanged(string filterName, string? value)
        {
            if (filterName == nameof(selectedSort))
            {
                selectedSort = value ?? "default";
                currentPage = 1;
                StateHasChanged();
                // Sorting logic if needed
            }
        }

       protected void OnClickCardItem(ClassifiedsIndex item)
        {
            NavigationManager.NavigateTo($"/qln/classifieds/preloved/details/{item.Id}");
        }
          protected int WindowWidth { get; set; }

        [Inject] protected IJSRuntime JS { get; set; } = default!;
        private DotNetObjectReference<PrelovedListSectionBase>? _objectRef;

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
            SearchState.PrelovedSearchText = null;
            SearchState.PrelovedCategory = null;
            SearchState.PrelovedBrand = null;
            SearchState.PrelovedMinPrice = null;
            SearchState.PrelovedMaxPrice = null;
            SearchState.PrelovedViewMode = "grid";
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
                new() { Label = "Preloved", Url = "/qln/classifieds/preloved", IsLast = true }
            };
        }
        protected async Task OnSortChanged(string newSortId)
        {
            selectedSort = newSortId;
            currentPage = 1;

            var selectedOption = sortOptions.FirstOrDefault(x => x.Id == newSortId);
            SearchState.PrelovedSortBy = selectedOption?.OrderByValue;

            if (OnSearch.HasDelegate)
            {
                await OnSearch.InvokeAsync(SearchState.PrelovedSearchText ?? string.Empty);
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
