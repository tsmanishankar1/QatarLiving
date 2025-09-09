using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Common.DTO_s;

namespace QLN.Web.Shared.Components.Classifieds.FeaturedItemCard
{
    public class FeaturedItemsBase : ComponentBase
    {
        [Parameter]
        public bool Loading { get; set; }

        [Parameter]
        public List<LandingFeaturedItemDto> FeaturedItems { get; set; } = new();

        [Inject]
        protected NavigationManager Navigation { get; set; } = default!;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        private bool _swiperInitialized = false;
        private bool _shouldInitSwiper = false;

        protected override Task OnParametersSetAsync()
        {
            if (!_swiperInitialized && !Loading && FeaturedItems?.Any() == true)
            {
                _shouldInitSwiper = true;
            }

            return Task.CompletedTask;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_shouldInitSwiper)
            {
                _swiperInitialized = true;
                _shouldInitSwiper = false;
                await JSRuntime.InvokeVoidAsync("initFeaturedSwiper");
            }
        }

        protected async Task HandleHeartClick(LandingFeaturedItemDto item)
        {
            Console.WriteLine($"Heart clicked for: {item.Title}");
            // Add business logic here
        }

        protected void HandleSelect(LandingFeaturedItemDto item)
        {
            Navigation.NavigateTo($"/qln/classifieds/items/details/{item.Id}");
        }
    }
}
