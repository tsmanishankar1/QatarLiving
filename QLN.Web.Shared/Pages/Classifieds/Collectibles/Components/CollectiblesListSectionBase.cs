using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Components.BreadCrumb;
using System.Collections.Generic;
using System.Linq;
using QLN.Common.DTO_s;

namespace QLN.Web.Shared.Pages.Classifieds.Collectibles.Components
{
    public class CollectiblesListSectionBase : ComponentBase
    {
        [Parameter] public string ViewMode { get; set; } = "grid";
        [Parameter] public bool Loading { get; set; } = false;

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

        protected void UpdateBreadcrumb(string uri)
        {
            breadcrumbItems = new()
            {
                new() { Label = "Classifieds", Url = "/qln/classifieds" },
                new() { Label = "Collectibles", Url = "/qln/classifieds/collectibles", IsLast = true }
            };
        }

        protected List<KeyValuePair<string, string>> sortOptions = new()
        {
            new("default", "Default"),
            new("priceLow", "Price: Low to High"),
            new("priceHigh", "Price: High to Low")
        };
    }
}
