using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Components.BreadCrumb;
using System.Collections.Generic;
using System.Linq;
using QLN.Common.DTO_s;

namespace QLN.Web.Shared.Pages.Classifieds.Items.Components
{
    public class ItemListSectionBase : ComponentBase
    {
        [Parameter] public string ViewMode { get; set; } = "grid";
        [Parameter] public bool Loading { get; set; } = false;
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
            NavigationManager.NavigateTo($"/qln/classifieds/items/details/{item.Id}");
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
        currentPage = 1;
        // Optionally do sorting logic
        await InvokeAsync(StateHasChanged);
    }

       public class SortOption
{
    public string Id { get; set; }
    public string Label { get; set; }
}
protected List<SortOption> sortOptions = new()
{
    new() { Id = "default", Label = "Default" },
    new() { Id = "priceLow", Label = "Price: Low to High" },
    new() { Id = "priceHigh", Label = "Price: High to Low" }
};

    }
}
