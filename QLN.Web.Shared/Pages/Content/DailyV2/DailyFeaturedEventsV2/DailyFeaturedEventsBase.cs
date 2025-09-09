using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Pages.Content.DailyV2
{
    public class DailyFeaturedEventsBase : ComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Parameter] public bool Loading { get; set; } = false;
        [Parameter] public List<ContentEvent> Items { get; set; } = [];

        private bool _categoriesSwiperInitialized = false;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_categoriesSwiperInitialized && !Loading && Items?.Any() == true)
            {
                _categoriesSwiperInitialized = true;
                await JSRuntime.InvokeVoidAsync("initCategoriesSwiper");
            }
        }

        protected void OnClickEvent(ContentEvent item)
        {
            NavigationManager.NavigateTo($"/content/events/details/{item.Slug}");
        }
    }
}
