using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace QLN.Web.Shared.Components.PriceRangeSelect
{
    public class PriceRangeSelectBase : ComponentBase, IDisposable
    {
        [Parameter] public long? SelectedMin { get; set; }
        [Parameter] public long? SelectedMax { get; set; }
        [Parameter] public EventCallback<long?> SelectedMinChanged { get; set; }
        [Parameter] public EventCallback<long?> SelectedMaxChanged { get; set; }
        [Parameter] public EventCallback OnApplyClicked { get; set; }
        [Parameter] public EventCallback OnResetClicked { get; set; }
        [Parameter] public bool IsDisabled { get; set; }

        protected string WrapperId => "price-range-wrapper";
        protected ElementReference WrapperRef;
        protected DotNetObjectReference<PriceRangeSelectBase>? dotNetRef;
        protected bool IsOpen { get; set; }
        protected long? MinValue;
        protected long? MaxValue;
        protected bool IsLoading = false;

        protected bool IsMobile = false;
        protected int windowWidth;
        protected bool _jsInitialized;
        protected const int MobileBreakpoint = 770;

        [Inject] protected IJSRuntime JS { get; set; } = default!;

        protected string FormattedMin
        {
            get => MinValue?.ToString("N0") ?? "";
            set => MinValue = ParseLong(value);
        }

        protected string FormattedMax
        {
            get => MaxValue?.ToString("N0") ?? "";
            set => MaxValue = ParseLong(value);
        }

        protected override void OnParametersSet()
        {
            MinValue = SelectedMin;
            MaxValue = SelectedMax;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                dotNetRef = DotNetObjectReference.Create(this);

                windowWidth = await JS.InvokeAsync<int>("getWindowWidth");
                IsMobile = windowWidth <= MobileBreakpoint;

                await JS.InvokeVoidAsync("toggleBodyScroll", IsOpen && IsMobile);
                await JS.InvokeVoidAsync("registerResizeHandler", dotNetRef);
                await JS.InvokeVoidAsync("registerOutsideClickHandlerPrice", $"#{WrapperId}", dotNetRef);

                _jsInitialized = true;
                StateHasChanged();
            }
        }

        [JSInvokable]
        public async Task UpdateWindowWidth(int width)
        {
            windowWidth = width;
            bool newIsMobile = windowWidth <= MobileBreakpoint;

            if (IsMobile != newIsMobile)
            {
                IsMobile = newIsMobile;
                await JS.InvokeVoidAsync("toggleBodyScroll", IsOpen && IsMobile);
                StateHasChanged();
            }
        }

        protected async Task HandleToggleClick()
        {
            if (IsDisabled) return;

            IsOpen = !IsOpen;

            if (_jsInitialized)
            {
                if (IsOpen)
                {
                    await JS.InvokeVoidAsync("registerOutsideClickHandlerPrice", $"#{WrapperId}", dotNetRef);
                }
                else
                {
                    await JS.InvokeVoidAsync("unregisterOutsideClickHandler", $"#{WrapperId}");
                }

                if (IsMobile)
                {
                    await JS.InvokeVoidAsync("toggleBodyScroll", IsOpen);
                }
            }
        }

        [JSInvokable]
        public async Task CloseDropdownFromJs()
        {
            IsOpen = false;

            if (_jsInitialized)
            {
                await JS.InvokeVoidAsync("unregisterOutsideClickHandler", $"#{WrapperId}");

                if (IsMobile)
                {
                    await JS.InvokeVoidAsync("toggleBodyScroll", false);
                }
            }

            StateHasChanged();
        }

        protected async Task ApplyValues()
        {
            IsLoading = true;
            try
            {
                await SelectedMinChanged.InvokeAsync(MinValue);
                await SelectedMaxChanged.InvokeAsync(MaxValue);
                await OnApplyClicked.InvokeAsync();
                IsOpen = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async void ResetValues()
        {
            MinValue = null;
            MaxValue = null;
            await OnResetClicked.InvokeAsync();
            IsOpen = false;
        }

        private long? ParseLong(string input)
        {
            if (long.TryParse(input.Replace(",", ""), out var result))
                return result;
            return null;
        }

        public string GetDisplayPriceText()
        {
            if (MinValue.HasValue && MaxValue.HasValue)
                return $"{FormatShort(MinValue)} - {FormatShort(MaxValue)}";
            if (MinValue.HasValue)
                return $"{FormatShort(MinValue)}";
            if (MaxValue.HasValue)
                return $"{FormatShort(MaxValue)}";
            return string.Empty;
        }

        private string FormatShort(long? value)
        {
            if (!value.HasValue) return "";

            var val = value.Value;
            if (val >= 1_00_00_000) return $"{val / 1_00_00_000.0:F1} Cr";
            if (val >= 1_00_000) return $"{val / 1_00_000.0:F1} L";
            if (val >= 1_000) return $"{val / 1_000.0:F1} K";
            return val.ToString("N0");
        }

        public void Dispose()
        {
            dotNetRef?.Dispose();

            if (_jsInitialized)
            {
                _ = JS.InvokeVoidAsync("unregisterOutsideClickHandler", $"#{WrapperId}");
            }
        }
    }
}
