using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;

namespace QLN.Web.Shared.Pages.Content.Daily.DailyIntheKnow
{
    public class DailyIntheKnowBase : LayoutComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject]
        protected IOptions<NavigationPath> Options { get; set; }

        protected NavigationPath NavigationPath => Options.Value;


        protected List<CategoryItem> DailyCategories =
        [
            new CategoryItem
            {
                Icon = "/qln-images/content/business_icon.svg",
                Label = "Business",
                QLCategLink = "forum/business-finance",
                CategorySlug = "business"
            },
            new CategoryItem
            {
                Icon = "/qln-images/content/sports_icon.svg",
                Label = "Sports",
                QLCategLink = "forum/worldcup",
                CategorySlug = "sports"
            },
            new CategoryItem
            {
                Icon = "/qln-images/content/lifestyle_icon.svg",
                Label = "Lifestyle",
                QLCategLink = "forum/fashion",
                CategorySlug = "lifestyle"
            },
            new CategoryItem
            {
                Icon = "/qln-images/content/food_icon.svg",
                Label = "Food & Dining",
                QLCategLink = "forum/dining",
                CategorySlug = "lifestyle",
                SubcategorySlug = "food-dining"
            },
            new CategoryItem
            {
                Icon = "/qln-images/content/travel_icon.svg",
                Label = "Travel & Leisure",
                QLCategLink = "travel-tourism",
                CategorySlug = "lifestyle",
                SubcategorySlug = "travel-leisure"
            }
        ];


        protected void OnClickCommunityCateg(CategoryItem item)
        {
            var uri = $"/content/news?category={item.CategorySlug}";

            if (!string.IsNullOrEmpty(item.SubcategorySlug))
            {
                uri += $"&subcategory={item.SubcategorySlug}";
            }

            NavigationManager.NavigateTo(uri);
        }


        protected void OnClickCommunityViewAll()
        {
            NavigationManager.NavigateTo("/content/news");
        }
    }
}
