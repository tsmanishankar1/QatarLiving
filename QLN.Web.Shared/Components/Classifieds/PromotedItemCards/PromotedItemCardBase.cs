using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Models;
using System.Text.Json;

namespace QLN.Web.Shared.Components.Classifieds.PromotedItemCards
{
    public class PromotedItemCardBase : ComponentBase
    {
        [Parameter] public PromotedItem Item { get; set; } = new();
        [Parameter] public EventCallback<PromotedItem> OnHeartClick { get; set; }

        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

        protected bool isHovered = false;
        protected bool isFavorite = false;
        protected int activeIndex = 0;

        protected async Task ToggleFavorite(PromotedItem item)
        {
            isFavorite = !isFavorite;
            await OnHeartClick.InvokeAsync(item);
        }

        protected async Task HandleHeartClick(PromotedItem item)
        {
            await ToggleFavorite(item);
        }

        protected Task HandleSelect(PromotedItem item)
{
    // Get the current base route like /classifieds/items
    var uri = new Uri(NavigationManager.Uri);
    var path = uri.AbsolutePath;
    var segments = path.Split("/", StringSplitOptions.RemoveEmptyEntries);

    string category = segments.Length >= 2 ? segments[1] : "items"; // fallback to "items"
    NavigationManager.NavigateTo($"/classifieds/{category}/details?id={item.Id}");
    return Task.CompletedTask;
}


        protected string heartIconListClass => isFavorite ? "heart-icon-fav filled" : "heart-icon-fav outlined";
        protected string heartIconClass => isFavorite ? "heart-icon filled" : "heart-icon outlined";

        protected void PrevImage()
        {
            if (Item.ImageUrls.Count == 0) return;
            activeIndex = (activeIndex - 1 + Item.ImageUrls.Count) % Item.ImageUrls.Count;
        }

        protected void NextImage()
        {
            if (Item.ImageUrls.Count == 0) return;
            activeIndex = (activeIndex + 1) % Item.ImageUrls.Count;
        }
    }
}
