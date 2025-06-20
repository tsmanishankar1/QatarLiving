using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Pages.Classifieds.Deals.Components
{
    public class DealsPreviewBase : ComponentBase
    {

        public List<string> imageUrls = new()
        {
            "/qln-images/classifieds/deals_preview_image1.svg",
            "/qln-images/classifieds/deals_preview_image2.svg",
            "/qln-images/classifieds/deals_preview_image3.svg",
        };
        public int currentIndex = 0;

        public void PreviousImage()
        {
            if (currentIndex > 0)
                currentIndex--;
        }

        public void NextImage()
        {
            if (currentIndex < imageUrls.Count - 1)
                currentIndex++;
        }
        protected void OnImageChanged(int index)
        {
            currentIndex = index;
        }
    }
}