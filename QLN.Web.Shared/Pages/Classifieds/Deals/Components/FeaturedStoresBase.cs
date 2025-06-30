using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Common.DTO_s;

namespace QLN.Web.Shared.Pages.Classifieds.Deals.Components
{
    public class FeaturedStoresBase : ComponentBase
    {
        
    public List<LandingBackOfficeIndex> Stores { get; set; } = new()
        {
new () { ImageUrl = "qln-images/stores/vector.svg" },
new () { ImageUrl = "qln-images/stores/starlink.svg" },
new () { Title = "MICROSOFT", ImageUrl = "qln-images/stores/microsoft.svg"},
new () { Title = "Lulu Hypermarket", ImageUrl = "qln-images/stores/hypermarket.svg" },
new () { Title = "City Hypermarket", ImageUrl = "qln-images/stores/hypermarket_city.svg" },
new () { Title = "Al Meera", ImageUrl = "qln-images/stores/meera.svg"},
new () { Title = "AIRBNB", ImageUrl = "qln-images/stores/vector.svg" },
new () { Title = "Starlink", ImageUrl = "qln-images/stores/starlink.svg" },
new () { Title = "Al Meera", ImageUrl = "qln-images/stores/meera.svg"},
new () { Title = "MICROSOFT", ImageUrl = "qln-images/stores/microsoft.svg" },
};


    protected void HandleShopNow(LandingBackOfficeIndex store)
    {
        Navigation.NavigateTo("/qln/classifieds/deals/details");
    }

        [Parameter] public bool Loading { get; set; }

        [Inject] protected IJSRuntime JS { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;

        private bool _swiperInitialized = false;
        private bool _shouldInitializeSwiper = false;

        /// <summary>
        /// Called when parent parameters (Stores/Loading) are set.
        /// Triggers Swiper init flag when data is ready.
        /// </summary>
        protected override Task OnParametersSetAsync()
        {
            if (!_swiperInitialized && !Loading && Stores is { Count: > 0 })
            {
                _shouldInitializeSwiper = true;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Called after the component is rendered.
        /// Triggers JavaScript Swiper init if flagged.
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_shouldInitializeSwiper)
            {
                _shouldInitializeSwiper = false;
                _swiperInitialized = true;

                await JS.InvokeVoidAsync("initDealsStoreSwiper");
            }
        }


        /// <summary>
        /// Allows manual Swiper reset if needed (optional).
        /// </summary>
        protected async Task ResetSwiperAsync()
        {
            _swiperInitialized = false;
            _shouldInitializeSwiper = true;
            await InvokeAsync(StateHasChanged); // Trigger render
        }
    }
}
