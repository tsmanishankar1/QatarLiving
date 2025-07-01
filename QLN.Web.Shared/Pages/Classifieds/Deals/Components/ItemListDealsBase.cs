using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using QLN.Web.Shared.Components.Classifieds.DealsCard;

namespace QLN.Web.Shared.Pages.Classifieds.Deals.Components
{
    public class ItemListDealsBase : ComponentBase
    {
        protected string selectedSort = "default";
        protected int currentPage = 1;
        protected int pageSize = 12;

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

[Inject]
protected NavigationManager Navigation { get; set; } = default!;

protected void OnDealClick()
{
    Navigation.NavigateTo("/qln/classifieds/deals/details/preview");
}

        protected List<DealsCard.DealModel> Deals = new()
        {
                new DealsCard.DealModel
                {
                ImageUrl = "qln-images/classifieds/deels_image.svg",
                StoreLogoUrl = "qln-images/stores/hypermarket.svg",
                StoreName = "Lulu Hypermarket",
                StoreDescription = "Ramadan Savers • 12 Pages",
                Location = "Al Sadd, West Bay +2",
                ExpiryText = "End in 2 Days"
                },
                new DealsCard.DealModel
                {
                ImageUrl = "qln-images/classifieds/deels_image.svg",
                StoreLogoUrl = "qln-images/stores/hypermarket.svg",
                StoreName = "Carlo Electronics",
                StoreDescription = "Mega Sale • 6 Pages",
                Location = "Salwa Road",
                ExpiryText = "Ends Tomorrow"
                },new DealsCard.DealModel
                {
                ImageUrl = "qln-images/classifieds/deels_image.svg",
                StoreLogoUrl = "qln-images/stores/hypermarket.svg",
                StoreName = "Lulu Hypermarket",
                StoreDescription = "Ramadan Savers • 12 Pages",
                Location = "Al Sadd, West Bay +2",
                ExpiryText = "End in 2 Days"
                },
                new DealsCard.DealModel
                {
                ImageUrl = "qln-images/classifieds/deels_image.svg",
                StoreLogoUrl = "qln-images/stores/hypermarket.svg",
                StoreName = "Carlo Electronics",
                StoreDescription = "Mega Sale • 6 Pages",
                Location = "Salwa Road",
                ExpiryText = "Ends Tomorrow"
                },new DealsCard.DealModel
                {
                ImageUrl = "qln-images/classifieds/deels_image.svg",
                StoreLogoUrl = "qln-images/stores/hypermarket.svg",
                StoreName = "Lulu Hypermarket",
                StoreDescription = "Ramadan Savers • 12 Pages",
                Location = "Al Sadd, West Bay +2",
                ExpiryText = "End in 2 Days"
                },
                new DealsCard.DealModel
                {
                ImageUrl = "qln-images/classifieds/deels_image.svg",
                StoreLogoUrl = "qln-images/stores/hypermarket.svg",
                StoreName = "Carlo Electronics",
                StoreDescription = "Mega Sale • 6 Pages",
                Location = "Salwa Road",
                ExpiryText = "Ends Tomorrow"
                }
            // Add additional deals as needed...
        };
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

        protected async Task OnSortChanged(string newSortId)
        {
            selectedSort = newSortId;
            currentPage = 1;

            var selectedOption = sortOptions.FirstOrDefault(x => x.Id == newSortId);
          
        }
        protected void HandlePageChange(int newPage)
        {
            currentPage = newPage;
        }

        protected void HandlePageSizeChange(int newSize)
        {
            pageSize = newSize;
        }
    }
}
