using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using System.Globalization;

namespace QLN.Web.Shared.Components.EventListCard
{
    public class EventListCardBase : ComponentBase
    {
        [Parameter]
        public ContentEvent Item { get; set; } = new();

        [Parameter]
        public EventCallback<ContentEvent> OnClick { get; set; }
        protected bool imageLoaded = false;

        protected string? currentImageUrl;

       protected bool imageFailed = false;

protected override void OnParametersSet()
{
    if (currentImageUrl != Item.ImageUrl)
    {
        currentImageUrl = Item.ImageUrl;
        imageLoaded = false;
        imageFailed = false;
    }
}

protected void OnImageLoaded()
{
    imageLoaded = true;
    imageFailed = false;
    StateHasChanged();
}

protected void OnImageError()
{
    imageLoaded = true; // stop skeleton
    imageFailed = true; // show fallback UI
    StateHasChanged();
}

        public class dto
        {
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public decimal PriceMin { get; set; }
            public decimal PriceMax { get; set; }
            public string ImageUrl { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }


        protected static string FormatDate(string dateString)
        {
            try
            {
                var parsed = DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                return parsed.ToString("dd MMM");
            }
            catch
            {
                return "Invalid date";
            }
        }
    }
}
