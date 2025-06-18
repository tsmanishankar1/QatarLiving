using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace QLN.Web.Shared.Components.CustomSelectCategory
{
    public class CustomSelectCategoryBase<TItem> : ComponentBase, IDisposable
    {
        [Parameter] public string Label { get; set; }
        [Parameter] public string Placeholder { get; set; } = "Choose";
        [Parameter] public List<TItem> Items { get; set; } = new();
        [Parameter] public string BorderRadius { get; set; } = "8px";
        [Parameter] public string FocusBorderColor { get; set; } = "#FF7F38";
        [Parameter] public string Padding { get; set; } = "10px";
        [Parameter] public string SelectedId { get; set; }
        [Parameter] public EventCallback<string> SelectedIdChanged { get; set; }
        [Parameter] public bool IsDisabled { get; set; } = false;
        [Parameter] public Func<TItem, string> GetLabel { get; set; }
        [Parameter] public Func<TItem, string> GetId { get; set; }

        protected bool IsOpen = false;
        protected ElementReference WrapperRef;
        protected DotNetObjectReference<CustomSelectCategoryBase<TItem>>? dotNetRef;
        protected bool IsMobile = false;
        protected int windowWidth;
        protected bool _jsInitialized;
        protected const int MobileBreakpoint = 770;

        protected string _tempSelectedId;

        [Inject] protected IJSRuntime JS { get; set; } = default!;

        protected string SelectedLabel => Items.FirstOrDefault(x => GetId(x) == SelectedId) is TItem selected
            ? GetLabel(selected)
            : Placeholder;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("registerResizeHandler", dotNetRef);

                windowWidth = await JS.InvokeAsync<int>("getWindowWidth");
                IsMobile = windowWidth <= MobileBreakpoint;

                await JS.InvokeVoidAsync("toggleBodyScroll", IsMobile);

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
                await JS.InvokeVoidAsync("toggleBodyScroll", IsMobile);
                StateHasChanged();
            }
        }

        protected void ToggleDropdown()
        {
            if (IsDisabled) return;
            IsOpen = !IsOpen;
            _tempSelectedId = SelectedId;
        }

        protected async Task SelectItem(TItem item)
        {
            SelectedId = GetId(item);
            IsOpen = false;
            await SelectedIdChanged.InvokeAsync(SelectedId);
        }

    protected void HandleFocusOut(FocusEventArgs e)
{
    if (!IsMobile)
    {
        IsOpen = false;
    }
}


        protected void HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" || e.Key == " " || e.Key == "ArrowDown")
            {
                ToggleDropdown();
            }
        }


         [JSInvokable]
        public void CloseBottomSheet()
        {
            IsOpen = false;
            StateHasChanged();
        }

        protected async Task ApplySelection()
        {
            SelectedId = _tempSelectedId;
            await SelectedIdChanged.InvokeAsync(SelectedId);
            IsOpen = false;
        }

        public void Dispose()
        {
            dotNetRef?.Dispose();
        }
    }
}
