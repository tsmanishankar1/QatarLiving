using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Components.ViewToggleButtons;
using MudBlazor;
using QLN.Common.DTO_s;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Routing;
using QLN.Web.Shared.Components;

namespace QLN.Web.Shared.Pages.Classifieds.Preloved.Components
{
    public class PrelovedSearchSectionBase : QLComponentBase
    {
            [Inject] protected SearchStateService SearchState { get; set; }
            protected bool ShowSaveSearchPopup { get; set; } = false;

            [Parameter] public EventCallback<string> OnSearch { get; set; }

            [Parameter] public EventCallback<string> OnViewModeChanged { get; set; }
            [Inject] private ILogger<PrelovedSearchSectionBase> Logger { get; set; }
            [Inject] private NavigationManager Nav { get; set; }
            protected bool _isSearchFocused = false;
            [Parameter]
            public EventCallback<string> SaveSearchAsync { get; set; }
            [Parameter]
            public bool IsLoadingSaveSearch { get; set; } = false;
            [Parameter] public bool Loading { get; set; } = false;
            [Parameter]
            public bool IsSaveSearch { get; set; } = false;
            protected List<ViewToggleButtons.ViewToggleOption> _viewOptions = new()
    {
        new() { ImageUrl = "/qln-images/list_icon.svg", Label = "List", Value = "list" },
        new() { ImageUrl = "/qln-images/grid_icon.svg", Label = "Grid", Value = "grid" }
    };
            protected List<CategoryTreeDto> CategoryTrees => SearchState.PrelovedCategoryTrees;
            protected CategoryTreeDto SelectedCategory =>
                CategoryTrees.FirstOrDefault(x => x.Id.ToString() == SearchState.PrelovedCategory)
                ?? new CategoryTreeDto { Children = new List<CategoryTreeDto>(), Fields = new List<CategoryField>() };

            protected CategoryTreeDto SelectedSubCategory =>
                SelectedCategory.Children?.FirstOrDefault(x => x.Id.ToString() == SearchState.PrelovedSubCategory)
                ?? new CategoryTreeDto { Children = new List<CategoryTreeDto>(), Fields = new List<CategoryField>() };

            protected CategoryTreeDto SelectedSubSubCategory =>
                SelectedSubCategory.Children?.FirstOrDefault(x => x.Id.ToString() == SearchState.PrelovedSubSubCategory)
                ?? new CategoryTreeDto { Children = new List<CategoryTreeDto>(), Fields = new List<CategoryField>() };

            protected List<CategoryField> SelectedFields
            {
                get
                {
                    if (SelectedSubSubCategory?.Fields?.Any() == true)
                        return SelectedSubSubCategory.Fields;

                    if (SelectedSubCategory?.Fields?.Any() == true)
                        return SelectedSubCategory.Fields;

                    if (SelectedCategory?.Fields?.Any() == true)
                        return SelectedCategory.Fields;

                    return new();
                }
            }

            protected CategoryField? brandField;
            protected bool isBrandFieldAvailable;
            protected override void OnParametersSet()
            {
                // ONLY check brand field from SelectedSubCategory
                var brandFromSubCategory = SelectedSubCategory.Fields?.FirstOrDefault(f =>
                    f.Name?.Trim().Equals("Brands", StringComparison.OrdinalIgnoreCase) == true &&
                    f.Type?.Trim().ToLower() == "dropdown");

                var brandFromSubSubCategory = SelectedSubSubCategory.Fields?.FirstOrDefault(f =>
                    f.Name?.Trim().Equals("Brands", StringComparison.OrdinalIgnoreCase) == true &&
                    f.Type?.Trim().ToLower() == "dropdown");

                // Show brand ONLY if it's in subcategory and NOT in sub-subcategory
                brandField = (brandFromSubCategory != null && brandFromSubSubCategory == null) ? brandFromSubCategory : null;
                isBrandFieldAvailable = brandField?.Options?.Any() == true;
            }

            protected Task HandleSecondaryClick()
            {
                if (IsSaveSearch)
                {
                    // Nav.NavigateTo("/qln/classifieds/saved-searches");
                    ShowSaveSearchPopup = false;
                }
                else
                {
                    ShowSaveSearchPopup = false;
                }

                return Task.CompletedTask;
            }
            protected async Task HandlePrimaryClick(string searchName)
            {
                await AuthorizedPage();
                if (IsSaveSearch)
                {
                    ShowSaveSearchPopup = false;
                }
                else
                {
                    await HandleSaveSearch(searchName);
                }
            }

            protected async Task HandleSaveSearch(string searchName)
            {
                if (SaveSearchAsync.HasDelegate)
                {
                    await SaveSearchAsync.InvokeAsync(searchName);
                }
            }
            protected override void OnInitialized()
            {
                Nav.LocationChanged += OnLocationChanged;
            }

            private void OnLocationChanged(object sender, LocationChangedEventArgs args)
            {
                var path = new Uri(args.Location).AbsolutePath.ToLowerInvariant();

                // Keep state if we're still under /qln/classifieds/preloved or its subpaths
                if (!path.StartsWith("/qln/classifieds/preloved"))
                {
                    SearchState.PrelovedSearchText = null;
                    SearchState.PrelovedCategory = null;
                    SearchState.PrelovedBrand = null;
                    SearchState.PrelovedMinPrice = null;
                    SearchState.PrelovedMaxPrice = null;
                    SearchState.PrelovedViewMode ??= "grid";
                    SearchState.PrelovedSubCategory = null;
                    SearchState.PrelovedSubSubCategory = null;
                    SearchState.PrelovedFilters.Clear();
                    SearchState.PrelovedHasWarrantyCertificate = false;
                    SearchState.PrelovedSetPage(1);
                    StateHasChanged();
                }
            }

            protected void OnFilterChanged(string fieldName, string value)
            {
                var prop = SearchState.GetType().GetProperty(fieldName);
                prop?.SetValue(SearchState, value);
                PerformSearch();
            }
            protected async Task PerformSearch()
            {
                SearchState.PrelovedSetPage(1);
                StateHasChanged();
                await Task.Yield();
                await OnSearch.InvokeAsync(SearchState.PrelovedSearchText);
            }
            protected async Task ClearSearch()
            {
                SearchState.PrelovedSearchText = string.Empty;
                StateHasChanged();

                if (OnSearch.HasDelegate)
                {
                    await OnSearch.InvokeAsync(string.Empty); // pass empty string as the search text
                }
            }
            protected async Task OnCategorySelected(string categoryId)
            {
                var category = CategoryTrees.FirstOrDefault(c => c.Id.ToString() == categoryId);
                if (category != null)
                {
                    SearchState.PrelovedCategory = category.Id.ToString();
                    SearchState.PrelovedSelectedCategoryName = category.Name;
                }

                SearchState.PrelovedSubCategory = null;
                SearchState.PrelovedSubSubCategory = null;
                SearchState.PrelovedSelectedSubCategoryName = null;
                SearchState.PrelovedSelectedSubSubCategoryName = null;
                SearchState.PrelovedBrand = null;
                SearchState.PrelovedSetPage(1);
                StateHasChanged();
                await PerformSearch();
            }

            protected async Task OnSubCategorySelected(string subId)
            {
                var subCategory = SelectedCategory?.Children?.FirstOrDefault(c => c.Id.ToString() == subId);
                if (subCategory != null)
                {
                    SearchState.PrelovedSubCategory = subCategory.Id.ToString();
                    SearchState.PrelovedSelectedSubCategoryName = subCategory.Name;
                }

                SearchState.PrelovedSubSubCategory = null;
                SearchState.PrelovedSelectedSubSubCategoryName = null;
                SearchState.PrelovedBrand = null;
                SearchState.PrelovedSetPage(1);
                StateHasChanged();
                await PerformSearch();
            }
            protected async Task OnSubSubCategorySelected(string subSubId)
            {
                var subSubCategory = SelectedSubCategory?.Children?.FirstOrDefault(c => c.Id.ToString() == subSubId);
                if (subSubCategory != null)
                {
                    SearchState.PrelovedSubSubCategory = subSubCategory.Id.ToString();
                    SearchState.PrelovedSelectedSubSubCategoryName = subSubCategory.Name;
                }

                SearchState.PrelovedBrand = null;
                SearchState.PrelovedSetPage(1);
                StateHasChanged();
                await PerformSearch();
            }

            protected void SetViewMode(string mode)
            {
                SearchState.PrelovedViewMode = mode;
                OnViewModeChanged.InvokeAsync(mode);
            }

        }
    }
