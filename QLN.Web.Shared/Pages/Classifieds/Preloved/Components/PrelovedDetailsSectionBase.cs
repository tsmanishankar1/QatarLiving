using Microsoft.AspNetCore.Components;
using QLN.Common.DTO_s;
using QLN.Web.Shared.Components.BreadCrumb;
using System;
using System.Collections.Generic;

namespace QLN.Web.Shared.Pages.Classifieds.Preloved.Components
{
    public class PrelovedDetailsSectionBase : ComponentBase
    {
        protected bool isSaved = false;
        protected List<BreadcrumbItem> breadcrumbItems = new();
        protected int _startIndex = 0;

        [Parameter]
        public ClassifiedsIndex Item { get; set; } = new();

        protected int selectedImageIndex = 0;
        protected string categorySegment = "items"; // Default

        protected override void OnParametersSet()
        {
            if (Item == null)
            {
                throw new InvalidOperationException("Item is null. The component cannot render.");
            }
        }

        protected override void OnInitialized()
        {
            breadcrumbItems = new()
            {
                new BreadcrumbItem { Label = "Classifieds", Url = "/qln/classifieds" },
                new BreadcrumbItem { Label = "Preloved", Url = "/qln/classifieds/preloved" },
                new BreadcrumbItem { Label = Item?.Title ?? "Details", Url = "/qln/classifieds/preloved/details", IsLast = true }
            };
        }

        protected void OnSaveClicked()
        {
            isSaved = !isSaved;
        }

        protected string HeartIcon => isSaved
            ? "qln-images/classifieds/liked_heart.svg"
            : "qln-images/classifieds/heart.svg";
    }
}
