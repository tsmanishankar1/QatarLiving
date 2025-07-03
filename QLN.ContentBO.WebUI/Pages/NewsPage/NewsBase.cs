using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.NewsPage
{
    public class NewsBase : QLComponentBase, IDisposable
    {
        [Inject] INewsService newsService { get; set; }
        [Inject] ILogger<NewsBase> Logger { get; set; }
        [Inject] protected NavigationManager Navigation { get; set; }

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

        public List<NewsArticleDTO> ListOfNewsArticles { get; set; }

        protected async override Task OnInitializedAsync()
        {
            Navigation.LocationChanged += HandleLocationChanged;

            ListOfNewsArticles = await GetAllArticles();

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

        protected List<PostItem> _posts = [.. Enumerable.Range(1, 12).Select(i => new PostItem
        {
            Number = i,
            PostTitle = "Family Residence Visa status stuck “Under Review”",
            CreationDate = new DateTime(2025, 4, 12),
            Username = "Ismat Zerin",
            LiveFor = "2 hours"
        })];

        protected void DeletePost(Guid Id)
        {
            ListOfNewsArticles.RemoveAll(a => a.Id == Id);
        }

        protected async Task<List<NewsArticleDTO>> GetAllArticles()
        {
            try
            {
                var apiResponse = await newsService.GetAllArticles();
                if (apiResponse.IsSuccessStatusCode)
                {
                    return await apiResponse.Content.ReadFromJsonAsync<List<NewsArticleDTO>>() ?? [];
                }

                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetAllArticles");
                return [];
            }
        }

        public class PostItem
        {
            public int Number { get; set; }
            public string PostTitle { get; set; } = "";
            public DateTime CreationDate { get; set; }
            public string Username { get; set; } = "";
            public string LiveFor { get; set; } = "";
        }
        protected Status status = Status.Live;

        protected Color GetButtonColor(Status s) => s == status ? Color.Warning : Color.Default;

        protected enum Status
        {
            Live,
            Published,
            Unpublished
        }
    }
}