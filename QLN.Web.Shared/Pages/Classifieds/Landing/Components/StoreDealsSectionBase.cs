using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Common.DTO_s;

namespace QLN.Web.Shared.Components.Classifieds.StoreCard
{
    public class StoreDealsSectionBase : ComponentBase
    {
        [Parameter] public List<LandingBackOfficeIndex> Stores { get; set; } = new();
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

                await JS.InvokeVoidAsync("initStoreSwiper");
            }
        }

        /// <summary>
        /// Handles store redirection logic.
        /// </summary>
        protected void HandleShopNow(LandingBackOfficeIndex store)
        {
            if (!string.IsNullOrWhiteSpace(store.RediectUrl))
            {
                Navigation.NavigateTo(store.RediectUrl);
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
