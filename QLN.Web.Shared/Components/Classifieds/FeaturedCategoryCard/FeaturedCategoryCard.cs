using Microsoft.AspNetCore.Components;

namespace QLN.Web.Shared.Components.Classifieds.FeaturedCategoryCard
{
    public partial class FeaturedCategoryCard : ComponentBase
    {
        [Parameter]
        public CategoryItem Item { get; set; } = new();

        [Parameter]
        public EventCallback<CategoryItem> OnClick { get; set; }

        public class CategoryItem
        {
            public string Category { get; set; } = string.Empty;
            public string ImageUrl { get; set; } = string.Empty;
        }
    }
}
