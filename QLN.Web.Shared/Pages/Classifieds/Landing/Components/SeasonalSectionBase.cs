using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Common.DTO_s;

namespace QLN.Web.Shared.Pages.Classifieds.Landing.Components
{
    public class SeasonalSectionBase : ComponentBase
    {
       [Parameter]
public List<LandingBackOfficeIndex> Seasonal { get; set; } = new();


        [Parameter]
        public bool Loading { get; set; }

        [Inject]
        protected IJSRuntime JS { get; set; } = default!;

        private bool _shouldInitSwiper = false;
        private bool _swiperInitialized = false;

        protected override Task OnParametersSetAsync()
        {
            if (!_swiperInitialized && Seasonal?.Any() == true && !Loading)
            {
                _shouldInitSwiper = true;
            }
            return Task.CompletedTask;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_shouldInitSwiper)
            {
                _shouldInitSwiper = false;
                _swiperInitialized = true;
                await JS.InvokeVoidAsync("initSeasonalSwiper");
            }
        }
    }
}
