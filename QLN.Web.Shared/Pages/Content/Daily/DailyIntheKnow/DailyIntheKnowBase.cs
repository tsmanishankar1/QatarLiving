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
        new CategoryItem { Icon = "/qln-images/content/business_icon.svg", Label = "Business", QLCategLink ="forum/business-finance" },
        new CategoryItem { Icon = "/qln-images/content/sports_icon.svg", Label = "Sports", QLCategLink ="forum/worldcup" },
        new CategoryItem { Icon = "/qln-images/content/lifestyle_icon.svg", Label = "Lifestyle", QLCategLink ="forum/fashion" },
        new CategoryItem { Icon = "/qln-images/content/food_icon.svg", Label = "Food & Dining",QLCategLink ="forum/dining" },
        new CategoryItem { Icon = "/qln-images/content/travel_icon.svg", Label = "Travel & Leisure", QLCategLink ="travel-tourism"},
        ];

        protected void OnClickNewsCateg(string qlNewsCategLink)
        {
            NavigationManager.NavigateTo($"{NavigationPath.Base}{qlNewsCategLink}");
        }

        protected void OnClickViewAll()
        {
            NavigationManager.NavigateTo("/content/news");
        }
    }
}
