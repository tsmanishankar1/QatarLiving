using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using Microsoft.AspNetCore.Components.Routing;

namespace QLN.ContentBO.WebUI.Pages.NewsPage
{
    public class NewsBase : ComponentBase, IDisposable
    {
        [Inject]
        protected NavigationManager Navigation { get; set; }
        protected int activeIndex = 0;
        protected string searchText;
        protected string selectedType;
        protected List<string> categories = new();
        protected Dictionary<string, List<string>> TypeCategoryMap = new()
        {
            { "news", new List<string> {"Qatar", "Middle East", "World", "Health & Education", "Community", "Law"} },
            { "finance", new List<string> { "Qatar Economy", "Market Updates", "Real Estate", "Entrepreneurship", "Finance", "Jobs & Careers" } },
            { "sports", new List<string> { "Qatar Sports", "Football", "International", "Motorsports", "Olympics", "Athlete Features" } },
            { "lifestyle", new List<string> { "Food & Dining", "Travel & Leisure", "Arts & Culture", "Events", "Fashion & Style", "Home & Living" } },
        };
        protected override void OnInitialized()
        {
            Navigation.LocationChanged += HandleLocationChanged;
            UpdateCategoriesFromQuery();
        }
        private void HandleLocationChanged(object sender, LocationChangedEventArgs e)
        {
            UpdateCategoriesFromQuery();
            InvokeAsync(StateHasChanged);
        }
        private void UpdateCategoriesFromQuery()
        {
            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("type", out var typeValue))
            {
                selectedType = typeValue!;
                if (TypeCategoryMap.TryGetValue(selectedType, out var list))
                {
                    categories = list;
                }
                else
                {
                    categories = new();
                }
            }
        }

        public void Dispose()
        {
            Navigation.LocationChanged -= HandleLocationChanged;
        }
         protected void NavigateToAddEvent()
        {
            Navigation.NavigateTo("/manage/news/addarticle");
        }
    }
}