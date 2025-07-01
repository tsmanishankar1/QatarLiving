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
            [Parameter] public Dictionary<string, Dictionary<string, bool>> SelectedOptions { get; set; } = new();

            [Parameter]
            public Dictionary<string, Dictionary<string, bool>> ConfirmedOptions { get; set; } = new();

            [Parameter]
         public bool HasWarrantyCertificate { get; set; }

        protected bool showFilters = false;
     protected int SelectedFilterCount => ConfirmedOptions
    .SelectMany(field => field.Value.Values)
    .Count(isSelected => isSelected)
    + (HasWarrantyCertificate ? 1 : 0);

        protected bool IsMobile = false;
        protected int windowWidth;
        protected bool _jsInitialized;
        protected const int MobileBreakpoint = 770;
        private IJSObjectReference? outsideClickHandler;

        [Inject] protected IJSRuntime JS { get; set; } = default!;
        protected DotNetObjectReference<MoreFiltersBase>? dotNetRef;




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

    if (showFilters && !wasOpen)
    {
        // Copy ConfirmedOptions to SelectedOptions
        SelectedOptions = ConfirmedOptions
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value.ToDictionary(entry => entry.Key, entry => entry.Value)
            );

        int openingCount = SelectedOptions
            .SelectMany(field => field.Value.Values)
            .Count(v => v);

        Console.WriteLine($"📂 [OpenFilters] Selected options on open: {openingCount}");
    }

    if (_jsInitialized)
    {
        await JS.InvokeVoidAsync("toggleBodyScroll", showFilters && IsMobile);

        if (showFilters && !IsMobile && !wasOpen)
        {
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
    ConfirmedOptions = SelectedOptions
        .ToDictionary(
            kv => kv.Key,
            kv => kv.Value.ToDictionary(entry => entry.Key, entry => entry.Value)
        );

    int appliedCount = ConfirmedOptions
        .SelectMany(field => field.Value.Values)
        .Count(v => v);

    Console.WriteLine($"✅ [ApplyFilters] Applied selected count: {appliedCount}");

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
    foreach (var group in SelectedOptions)
    {
        foreach (var key in group.Value.Keys.ToList())
        {
            SelectedOptions[group.Key][key] = false;
        }
    }

    foreach (var group in ConfirmedOptions)
    {
        foreach (var key in group.Value.Keys.ToList())
        {
            ConfirmedOptions[group.Key][key] = false;
        }
    }

    showFilters = false;


    await OnReset.InvokeAsync();

    StateHasChanged(); // Important: this makes sure the UI reflects 0 badge
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
