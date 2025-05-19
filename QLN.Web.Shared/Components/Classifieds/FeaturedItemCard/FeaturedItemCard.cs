using Microsoft.AspNetCore.Components;
using MudBlazor;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
namespace QLN.Web.Shared.Components.Classifieds.FeaturedItemCard
{
    public partial class FeaturedItemCard : ComponentBase
    {
            [Inject] protected IJSRuntime JS { get; set; }

        [Parameter]
        public FeaturedItem Item { get; set; } = new();

        [Parameter]
        public EventCallback<FeaturedItem> OnHeartClick { get; set; }
        protected bool imageLoaded = false;
            protected bool ImageLoaded { get; set; } = false;
    protected string ImageElementId { get; } = $"img_{Guid.NewGuid():N}";
        protected bool IsFavorite { get; set; } = false;

        protected string HeartIconClass => IsFavorite ? "heart-icon filled" : "heart-icon outlined";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("registerImageLoadCallback", ImageElementId, DotNetObjectReference.Create(this));
        }
    }

    [JSInvokable]
    public void OnImageLoaded()
    {
        ImageLoaded = true;
        StateHasChanged();
    }

        protected async Task HandleHeartClick(FeaturedItem item)
        {
            IsFavorite = !IsFavorite;
            await OnHeartClick.InvokeAsync(item);
        }

        public class FeaturedItem
        {
            public List<string> ImageUrls { get; set; } = new();
            public string Category { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public decimal? Price { get; set; }
            public string Location { get; set; } = string.Empty;
            public bool IsFeatured { get; set; }
        }
    }
}
