using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Linq;

namespace QLN.Web.Shared.Components.MoreFilters
{
    public class MoreFiltersBase : ComponentBase, IDisposable
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public EventCallback OnApply { get; set; }
        [Parameter] public EventCallback OnReset { get; set; }
        [Parameter] public Dictionary<string, bool> Selection { get; set; } = new();

        protected Dictionary<string, bool> confirmedSelection = new();
        protected bool showFilters = false;
        protected int SelectedFilterCount => confirmedSelection.Count(x => x.Value);

        protected bool IsMobile = false;
        protected int windowWidth;
        protected bool _jsInitialized;
        protected const int MobileBreakpoint = 770;
        private IJSObjectReference? outsideClickHandler;

        [Inject] protected IJSRuntime JS { get; set; } = default!;
        protected DotNetObjectReference<MoreFiltersBase>? dotNetRef;

        protected override void OnInitialized()
        {
            confirmedSelection = new Dictionary<string, bool>(Selection);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                dotNetRef = DotNetObjectReference.Create(this);

                windowWidth = await JS.InvokeAsync<int>("blazorResize.getWidth");
                IsMobile = windowWidth <= MobileBreakpoint;

                await JS.InvokeVoidAsync("blazorResize.registerResizeCallback", dotNetRef);
                await JS.InvokeVoidAsync("toggleBodyScroll", showFilters && IsMobile);

                _jsInitialized = true;
                StateHasChanged();
            }
        }
        protected async void ToggleFilter()
        {
            bool wasOpen = showFilters;
            showFilters = !showFilters;

            if (_jsInitialized)
            {
                await JS.InvokeVoidAsync("toggleBodyScroll", showFilters && IsMobile);

                if (showFilters && !IsMobile && !wasOpen)
                {
                    // Only register outside click if opening, and not already registered
                    if (outsideClickHandler == null)
                    {
                        outsideClickHandler = await JS.InvokeAsync<IJSObjectReference>(
                            "registerOutsideClickHandlerOut", ".filter-panel", dotNetRef);
                    }
                }
                else if (!showFilters)
                {
                    await DisposeOutsideClickHandler();
                }
            }

            StateHasChanged();
        }


        private async Task DisposeOutsideClickHandler()
        {
            if (outsideClickHandler != null)
            {
                await outsideClickHandler.InvokeVoidAsync("dispose");
                outsideClickHandler = null;
            }
        }

        protected async Task ApplyFilters()
        {
            confirmedSelection = new Dictionary<string, bool>(Selection);
            showFilters = false;
            await JS.InvokeVoidAsync("toggleBodyScroll", false);
            await DisposeOutsideClickHandler();
            await OnApply.InvokeAsync();
        }

        [JSInvokable]
        public async Task CloseFilters()
        {
            showFilters = false;

            if (IsMobile)
            {
                await JS.InvokeVoidAsync("toggleBodyScroll", false);
            }

            await DisposeOutsideClickHandler();
            StateHasChanged();
        }

        protected async Task ResetFilters()
        {
            foreach (var key in Selection.Keys.ToList())
            {
                Selection[key] = false;
            }

            await OnReset.InvokeAsync();
        }

        [JSInvokable]
        public async Task UpdateWindowWidth(int width)
        {
            windowWidth = width;
            bool newIsMobile = windowWidth <= MobileBreakpoint;

            if (IsMobile != newIsMobile)
            {
                IsMobile = newIsMobile;
                await JS.InvokeVoidAsync("toggleBodyScroll", showFilters && IsMobile);
                StateHasChanged();
            }
        }

        public void Dispose()
        {
            dotNetRef?.Dispose();
        }
    }
}
