using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Components;

namespace QLN.Web.Shared.Pages.Content.Daily
{
    public class DailyFeaturedEventsBase : QLComponentBase
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
            NavigationManager.NavigateTo($"{NavigationPath.Value.ContentEventsDetail}{item.Slug}");
        }
    }
}
