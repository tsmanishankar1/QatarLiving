using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.Text.Json;

namespace QLN.Web.Shared.Pages.Content.DailyV2.DailyIntheKnow
{
    public class DailyIntheKnowBase : LayoutComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }

        [Inject] private INewsService _newsService { get; set; }


        protected List<CategoryItem> DailyCategories { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            var response = await _newsService.GetAllNewsCategoriesAsync();
            if (response.IsSuccessStatusCode)
            {
                var contentString = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(contentString))
                {
                    var categoriesFromApi = JsonSerializer.Deserialize<List<NewsCategory>>(contentString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Define the ID pairs to map (main category ID, subcategory ID)
                    var targetPairs = new List<(int mainId, int subId)>
                    {
                        (102, 1007), // Business / Qatar Economy
                        (103, 1013), // Sports / Qatar Sports
                        (104, 1023), // Lifestyle / Fashion & Style
                        (104, 1019), // Lifestyle / Food and Dining
                        (104, 1020)  // Lifestyle / Travel & Leisure
                    };

                    // Map only the matched pairs into DailyCategories
                    DailyCategories = categoriesFromApi
                        .SelectMany(cat => cat.SubCategories
                            .Where(sub => targetPairs.Contains((cat.Id, sub.Id)))
                            .Select(sub => new CategoryItem
                            {
                                Icon = GetIconForSubCategory(sub.Id),
                                Label = sub.SubCategoryName,
                                CategorySlug = cat.CategoryName,
                                SubcategorySlug = sub.SubCategoryName,
                            }))
                        .ToList();
                }
            }
        }

        protected void OnClickCommunityCateg(CategoryItem item)
        {
            var uri = $"/content/v2/news?category={item.CategorySlug}";

            if (!string.IsNullOrEmpty(item.SubcategorySlug))
            {
                uri += $"&subcategory={item.SubcategorySlug}";
            }

            NavigationManager.NavigateTo(uri);
        }

        protected void OnClickCommunityViewAll()
        {
            NavigationManager.NavigateTo("/content/V2/news");
        }

        private string GetIconForSubCategory(int subId)
        {
            return subId switch
            {
                1007 => "/qln-images/content/business_icon.svg",
                1013 => "/qln-images/content/sports_icon.svg",
                1023 => "/qln-images/content/lifestyle_icon.svg",
                1019 => "/qln-images/content/food_icon.svg",
                1020 => "/qln-images/content/travel_icon.svg",
                _ => "/qln-images/default_icon.svg"
            };
        }
    }
}
