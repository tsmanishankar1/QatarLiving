using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Components.ViewToggleButtons;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;
using QLN.Web.Shared.Services.Interface;
using System.Text.Json;

namespace QLN.Web.Shared.Pages.Content.NewsV2
{
    public class NewsBaseV2 : ComponentBase
    {
        [Inject] IContentService _contentService { get; set; }
        [Inject] private ILogger<NewsCardBase> Logger { get; set; }
        [Inject] private INewsService _newsService { get; set; }
        [Inject] private IEventService _eventService { get; set; }
        [Inject] private ISimpleMemoryCache _simpleCacheService { get; set; }
        [Inject] protected NavigationManager navManager { get; set; }

        [Inject]
        protected IOptions<NavigationPath> NavigationPath { get; set; }

        protected bool isLoading = true;
        protected bool isLoadingBanners = true;
        protected bool imageFailed = false;
        protected bool imageLoaded = false;
        protected string? currentImageUrl;

        protected string _selectedView = "news";
        protected string selectedTabView = "News";
        protected string[] Tabs = Array.Empty<string>();
        protected string SelectedTab = string.Empty;
        protected string subTabLabel = string.Empty;
        protected string selectedRouterTab = string.Empty;

        public List<NewsCategory> NewsCategories { get; set; } = new();

        protected NewsContentV2? NewsContent { get; set; }

        protected List<BannerItem> DailyTakeOverBanners = new();
        protected List<BannerItem> DailyHeroBanners = new();
        protected List<BannerItem> NewsSideBanners { get; set; } = new();

        protected List<ContentVideo> mostWatchedArticleListSlot { get; set; } = new();

        protected List<ViewToggleButtons.ViewToggleOption> _viewOptions = new();
        protected List<ViewToggleButtons.ViewToggleOption> routerList = new();

        protected List<string> carouselImages = new()
        {
            "/images/banner_image.svg",
            "/images/banner_image.svg",
            "/images/banner_image.svg"
        };

        protected ContentPost? topNewsSlot => NewsContent?.News?.TopStory?.Items?.FirstOrDefault();
        protected List<ContentPost>? topNewsListSlot => NewsContent?.News?.TopStory?.Items;
        protected List<ContentPost>? moreArticleListSlot => NewsContent?.News?.MoreArticles?.Items;
        protected List<ContentPost>? articleListSlot1 => NewsContent?.News?.Articles1?.Items;
        protected List<ContentPost>? articleListSlot2 => NewsContent?.News?.Articles2?.Items;
        protected List<ContentPost>? popularArticleListSlot => NewsContent?.News?.MostPopularArticles?.Items;
        protected List<ContentVideo>? VideoList => NewsContent?.News?.WatchOnQatarLiving?.Items;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                isLoading = true;

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

                        if (categoriesFromApi != null && categoriesFromApi.Any())
                        {
                            NewsCategories = categoriesFromApi;

                            _viewOptions = NewsCategories.Select(cat => new ViewToggleButtons.ViewToggleOption
                            {
                                Label = cat.CategoryName,
                                Value = cat.Id.ToString()
                            }).ToList();

                            routerList = NewsCategories
                                .SelectMany(cat => cat.SubCategories.Select(sub => new ViewToggleButtons.ViewToggleOption
                                {
                                    Label = sub.SubCategoryName,
                                    Value = sub.Id.ToString()
                                })).ToList();

                            var (categoryParam, subCategoryParam) = ParseQueryParams();

                            var selectedCategory = NewsCategories.FirstOrDefault(c => c.CategoryName == categoryParam) ?? NewsCategories.First();
                            _selectedView = selectedCategory.Id.ToString();
                            selectedTabView = selectedCategory.CategoryName;
                            Tabs = selectedCategory.SubCategories.Select(sc => sc.SubCategoryName).ToArray();

                            var selectedSub = selectedCategory.SubCategories.FirstOrDefault(sc => sc.SubCategoryName == subCategoryParam)
                                              ?? selectedCategory.SubCategories.First();

                            SelectedTab = selectedSub.SubCategoryName;
                            subTabLabel = selectedSub.Id.ToString();

                            await LoadNewsContent(selectedCategory.Id, selectedSub.Id);
                        }
                    }
                }

                await base.OnInitializedAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync Error");
            }
            finally
            {
                isLoading = false;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            try
            {
                isLoading = true;
                await Task.WhenAll(
                    LoadBanners(SelectedTab),
                    LoadInitialData()
                );
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnAfterRenderAsync error");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task LoadInitialData()
        {
            if (int.TryParse(_selectedView, out var categoryId) && int.TryParse(subTabLabel, out var subCategoryId))
            {
                await LoadNewsContent(categoryId, subCategoryId);
            }
        }

        protected async Task LoadNewsContent(int categoryId, int subCategoryId)
        {
            try
            {
                var response = await _newsService.GetNewsBySubCategoryAsync(categoryId, subCategoryId);
                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    NewsContent = JsonSerializer.Deserialize<NewsContentV2>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                else
                {
                    Logger.LogWarning("News API returned no content.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading news content");
            }
        }

        protected async void SelectTab(string tab)
        {
            isLoading = true;
            try
            {
                SelectedTab = tab;

                var currentCategory = NewsCategories.FirstOrDefault(c => c.Id.ToString() == _selectedView);
                if (currentCategory != null)
                {
                    var subCategory = currentCategory.SubCategories.FirstOrDefault(sc => sc.SubCategoryName == tab);
                    if (subCategory != null)
                    {
                        subTabLabel = subCategory.Id.ToString();
                        await LoadNewsContent(currentCategory.Id, subCategory.Id);
                        selectedRouterTab = subCategory.SubCategoryName;

                        navManager.NavigateTo($"/content/V2/news?category={selectedTabView}&subcategory={selectedRouterTab}", forceLoad: false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SelectTab error");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        protected async void SetViewMode(string view)
        {
            _selectedView = view;

            var selectedCategory = NewsCategories.FirstOrDefault(c => c.Id.ToString() == view);
            if (selectedCategory != null)
            {
                selectedTabView = selectedCategory.CategoryName;
                Tabs = selectedCategory.SubCategories.Select(sc => sc.SubCategoryName).ToArray();

                var targetSub = string.IsNullOrEmpty(selectedRouterTab)
                    ? selectedCategory.SubCategories.FirstOrDefault()
                    : selectedCategory.SubCategories.FirstOrDefault(sc => sc.SubCategoryName == selectedRouterTab);

                if (targetSub != null)
                {
                    SelectedTab = targetSub.SubCategoryName;
                    subTabLabel = targetSub.Id.ToString();
                    await LoadNewsContent(selectedCategory.Id, targetSub.Id);

                    navManager.NavigateTo($"/content/V2/news?category={selectedTabView}&subcategory={SelectedTab}", forceLoad: false);
                }
            }
            else
            {
                Tabs = Array.Empty<string>();
            }
        }

        protected async Task LoadBanners(string tab)
        {
            isLoadingBanners = true;
            try
            {
                var banners = await _simpleCacheService.GetBannerAsync();
                NewsSideBanners = banners?.ContentNewsSide ?? new();
                DailyHeroBanners = banners?.ContentNewsHero ?? new();
                DailyTakeOverBanners = banners?.ContentNewsTakeover ?? new();
            }
            finally
            {
                isLoadingBanners = false;
            }
        }

        protected void OnImageLoaded()
        {
            imageLoaded = true;
            imageFailed = false;
            StateHasChanged();
        }

        protected void OnImageError()
        {
            imageLoaded = true;
            imageFailed = true;
            StateHasChanged();
        }

        protected override void OnParametersSet()
        {
            imageLoaded = false;
        }

        protected void onclick(ContentPost news)
        {
            if (!string.IsNullOrEmpty(selectedTabView) && !string.IsNullOrEmpty(SelectedTab))
            {
                navManager.NavigateTo($"/content/V2/article/details/{news.Slug}?category={selectedTabView}&subcategory={SelectedTab}");
            }
            else if (!string.IsNullOrEmpty(selectedTabView))
            {
                navManager.NavigateTo($"/content/V2/article/details/{news.Slug}?category={selectedTabView}");
            }
            else
            {
                navManager.NavigateTo($"/content/V2/article/details/{news.Slug}");
            }
        }

        protected string getLink(ContentPost news)
        {
            if (news == null) return "";

            if (!string.IsNullOrEmpty(selectedTabView) && !string.IsNullOrEmpty(SelectedTab))
            {
                return $"{NavigationPath.Value.ContentNewsDetail}/{news.Slug}?category={selectedTabView}&subcategory={SelectedTab}";
            }
            else if (!string.IsNullOrEmpty(selectedTabView))
            {
                return $"{NavigationPath.Value.ContentNewsDetail}/{news.Slug}?category={selectedTabView}";
            }
            else
            {
                return $"{NavigationPath.Value.ContentNewsDetail}/{news.Slug}";
            }
        }

        private (string? Category, string? SubCategory) ParseQueryParams()
        {
            var uri = navManager.ToAbsoluteUri(navManager.Uri);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var category = query["category"];
            var subcategory = query["subcategory"];
            return (category, subcategory);
        }
    }
}
