using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using System.Globalization;

namespace QLN.Web.Shared.Components.EventListCardV2
{
    public class EventListCardBaseV2 : ComponentBase
    {
        [Parameter]
        public EventDTOV2 Item { get; set; } = new();

        [Parameter]
        public EventCallback<EventDTOV2> OnClick { get; set; }
        protected bool imageLoaded = false;

        protected string? currentImageUrl;

       protected bool imageFailed = false;

    protected override void OnParametersSet()
    {
        if (currentImageUrl != Item.CoverImage)
        {
            currentImageUrl = Item.CoverImage;
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
