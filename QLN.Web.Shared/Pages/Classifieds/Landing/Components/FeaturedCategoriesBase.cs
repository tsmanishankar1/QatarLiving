using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Common.DTO_s;

namespace QLN.Web.Shared.Components.Classifieds.FeaturedCategoryCard
{
    public class FeaturedCategoriesBase : ComponentBase
    {
        [Parameter]
        public List<LandingBackOfficeIndex> FeaturedCategories { get; set; } = new();

        [Parameter]
        public bool Loading { get; set; }

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;
       [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        private bool _swiperInitialized = false;
        private bool _shouldInitSwiper = false;

        protected override Task OnParametersSetAsync()
        {
            if (!_swiperInitialized && !Loading && FeaturedCategories?.Count > 0)
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

                await JSRuntime.InvokeVoidAsync("initCategoriesSwiper");
            }
        }

          protected void HandleCategoryClick(LandingBackOfficeIndex category)
         {
        if (category?.EntityId == null)
            return;

        var url = $"/qln/classifieds/items?categoryIdL1={category.EntityId}";
        NavigationManager.NavigateTo(url);
    }
    }
}
