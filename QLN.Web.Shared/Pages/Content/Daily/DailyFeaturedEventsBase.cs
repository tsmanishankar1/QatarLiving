using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Common.Infrastructure.DTO_s;
using static MudBlazor.CategoryTypes;

namespace QLN.Web.Shared.Pages.Content.Daily
{
    public class DailyFeaturedEventsBase : ComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Parameter] public bool Loading { get; set; } = false;
        [Parameter] public List<ContentPost> Items { get; set; } = [];

        private bool _categoriesSwiperInitialized = false;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_categoriesSwiperInitialized)
            {
                _categoriesSwiperInitialized = true;
                await JSRuntime.InvokeVoidAsync("initCategoriesSwiper");
            }
        }

        protected void OnClickEvent(ContentPost item)
        {
            NavigationManager.NavigateTo($"/events/details/{item.Slug}");
        }
    }
}
