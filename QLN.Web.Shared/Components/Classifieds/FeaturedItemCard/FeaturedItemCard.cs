using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace QLN.Web.Shared.Components.Classifieds.FeaturedItemCard
{
    public partial class FeaturedItemCard : ComponentBase
    {
        [Parameter]
        public FeaturedItem Item { get; set; } = new();

        [Parameter]
        public EventCallback<FeaturedItem> OnHeartClick { get; set; }

        protected bool IsFavorite { get; set; } = false;

        protected string HeartIconClass => IsFavorite ? "heart-icon filled" : "heart-icon outlined";

        protected async Task HandleHeartClick(FeaturedItem item)
        {
            IsFavorite = !IsFavorite;
            await OnHeartClick.InvokeAsync(item);
        }

        public class FeaturedItem
        {
            public string ImageUrl { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Price { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public bool IsFeatured { get; set; }
        }
    }
}
