using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Pages.Content.NewsV2.NewsCard
{
    public class NewsCardV2Base : ComponentBase
    {
        [Inject]
        protected NavigationManager navManager { get; set; }
        [Parameter]
        public ContentPost news { get; set; } = new ContentPost();
        protected bool imageLoaded = false;
        protected bool imageFailed = false;
        protected string? currentImageUrl;
        protected override void OnParametersSet()
        {
            if (currentImageUrl != news.ImageUrl)
            {
                currentImageUrl = news.ImageUrl;
                imageLoaded = false;
                imageFailed = false;
            }
        }
        protected void OnImageLoaded()
        {
            imageLoaded = true;
            StateHasChanged();
        }
        protected void OnImageError()
        {
            imageLoaded = true;
            imageFailed = false;
            StateHasChanged();
        }
        protected bool ShowEmptyCard =>
            string.IsNullOrWhiteSpace(news?.ImageUrl) || imageFailed;


        [Parameter]
        public bool IsHorizontal { get; set; } = false;

        [Parameter] public string Href { get; set; } = "";
    }
}