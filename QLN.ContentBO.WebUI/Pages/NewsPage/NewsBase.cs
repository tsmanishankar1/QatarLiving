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

        [Parameter] public int CategoryId { get; set; }

        protected int activeIndex = 0;

        protected string searchText;

        protected string selectedType;

        protected Dictionary<string, List<string>> TypeCategoryMap = new()
        {
            { "news", new List<string> {"Qatar", "Middle East", "World", "Health & Education", "Community", "Law"} },
            { "finance", new List<string> { "Qatar Economy", "Market Updates", "Real Estate", "Entrepreneurship", "Finance", "Jobs & Careers" } },
            { "sports", new List<string> { "Qatar Sports", "Football", "International", "Motorsports", "Olympics", "Athlete Features" } },
            { "lifestyle", new List<string> { "Food & Dining", "Travel & Leisure", "Arts & Culture", "Events", "Fashion & Style", "Home & Living" } },
        };

        public List<NewsArticleDTO> ListOfNewsArticles { get; set; }

        protected List<Slot> Slots = [];

        protected List<NewsCategory> Categories = [];

        protected List<NewsSubCategory> SubCategories = [];

        protected int SelectedSubcategoryId { get; set; } = 1;

        protected async override Task OnInitializedAsync()
        {
            Categories = await GetNewsCategories() ?? [];
            SubCategories = Categories.Where(c => c.Id == CategoryId)?.FirstOrDefault()?.SubCategories ?? [];

            ListOfNewsArticles = await GetNewsBySubCategories(CategoryId, SelectedSubcategoryId) ?? [];

            Slots = await GetSlots();
        }

        private void HandleLocationChanged(object sender, LocationChangedEventArgs e)
        {
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            Navigation.LocationChanged -= HandleLocationChanged;
        }

        protected void NavigateToAddEvent()
        {
            Navigation.NavigateTo("/manage/news/addarticle");
        }

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

        protected async void Click_MoveItemUp(Guid Id)
        {
            try
            {
                var articleToUpdate = ListOfNewsArticles.FirstOrDefault(a => a.Id.Equals(Id)) ?? new();

                var apiResponse = await newsService.UpdateArticle(articleToUpdate);
                if (apiResponse.IsSuccessStatusCode)
                {
                    Snackbar.Add("Slot Updated");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Click_MoveItemUp");
            }
        }

        protected async void Click_MoveItemDown(Guid Id)
        {
            try
            {
                var articleToUpdate = ListOfNewsArticles.FirstOrDefault(a => a.Id.Equals(Id)) ?? new();

                var apiResponse = await newsService.UpdateArticle(articleToUpdate);
                if (apiResponse.IsSuccessStatusCode)
                {
                    Snackbar.Add("Slot Updated");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Click_MoveItemUp");
            }
        }

        private async Task<List<Slot>> GetSlots()
        {
            try
            {
                var apiResponse = await newsService.GetSlots();
                if (apiResponse.IsSuccessStatusCode)
                {
                    return await apiResponse.Content.ReadFromJsonAsync<List<Slot>>() ?? [];
                }

                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetSlots");
                return [];
            }
        }


        protected async Task<List<NewsArticleDTO>> GetNewsByCategories(int categoryId)
        {
            try
            {
                var apiResponse = await newsService.GetArticlesByCategory(categoryId);
                if (apiResponse.IsSuccessStatusCode)
                {
                    return await apiResponse.Content.ReadFromJsonAsync<List<NewsArticleDTO>>() ?? [];
                }

                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsByCategories");
                return [];
            }
        }

        protected async Task<List<NewsArticleDTO>> GetNewsBySubCategories(int categoryId, int subCategoryId)
        {
            try
            {
                var apiResponse = await newsService.GetArticlesBySubCategory(categoryId, subCategoryId);
                if (apiResponse.IsSuccessStatusCode)
                {
                    return await apiResponse.Content.ReadFromJsonAsync<List<NewsArticleDTO>>() ?? [];
                }

                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsBySubCategories");
                return [];
            }
        }

        protected async void LoadCategory(int categoryId, int subCategory)
        {
            SelectedSubcategoryId = subCategory;
            ListOfNewsArticles = await GetNewsBySubCategories(categoryId, subCategory) ?? [];
        }

        private async Task<List<NewsCategory>> GetNewsCategories()
        {
            try
            {
                var apiResponse = await newsService.GetNewsCategories();
                if (apiResponse.IsSuccessStatusCode)
                {
                    return await apiResponse.Content.ReadFromJsonAsync<List<NewsCategory>>() ?? [];
                }

                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsCategories");
                return [];
            }
        }

        protected string GetCategoryName(int categoryId) 
        {
            return Categories.FirstOrDefault(c => c.Id == categoryId)?.CategoryName ?? "Qatar";
        }

        protected string? GetSubCategoryName(int CategoryId, int subCategoryId)
        {
            return Categories
                .FirstOrDefault(c => c.Id == CategoryId)?
                .SubCategories
                .FirstOrDefault(sc => sc.Id == subCategoryId)?
                .CategoryName;
        }
    }
}