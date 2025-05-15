using Microsoft.AspNetCore.Components;

namespace QLN.Web.Shared.Components.Classifieds.CategoryAdCard
{
    public partial class CategoryAdCard : ComponentBase
    {
        [Parameter]
        public CategoryItem Item { get; set; } = new();

        [Parameter]
        public EventCallback<CategoryItem> OnClick { get; set; }

        public class CategoryItem
        {
            public string Title { get; set; } = string.Empty;
            public string Subtitle { get; set; } = string.Empty;
            public string ImageUrl { get; set; } = string.Empty;
        }
    }
}
