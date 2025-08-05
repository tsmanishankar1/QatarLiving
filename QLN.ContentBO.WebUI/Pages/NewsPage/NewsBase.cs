using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.News;
using QLN.ContentBO.WebUI.Extensions;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services;
using System.Net;
using System.Text.Json;
using static QLN.ContentBO.WebUI.Components.ToggleTabs.ToggleTabs;

namespace QLN.ContentBO.WebUI.Pages.NewsPage
{
    public class NewsBase : QLComponentBase
    {
        [Inject] protected INewsService newsService { get; set; }
        [Inject] protected ILogger<NewsBase> Logger { get; set; }
        [Inject] protected NavigationManager Navigation { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }

        [Parameter] public int CategoryId { get; set; }

        protected int activeIndex = 0;

        protected string selectedType;

        public List<NewsArticleDTO> ListOfNewsArticles { get; set; } = [];

        protected List<Slot> Slots = [];

        protected List<NewsCategory> Categories = [];

        protected List<NewsSubCategory> SubCategories = [];

        protected NewsSubCategory SelectedSubcategory { get; set; } = new();

        protected ArticleSlotAssignment articleSlotAssignment { get; set; } = new();

        protected bool IsEditingSubCategoryName { get; set; } = false;

        protected NewsSubCategory EditableSubCategoryName { get; set; } = new();

        protected MudTextField<string> subCategoryInputRef;
        protected bool shouldFocusInput { get; set; } = false;

        protected List<TabOption> tabOptions = new()
        {
            new() { Label = "Live", Value = "live" },
            new() { Label = "Published", Value = "published" },
            new() { Label = "Unpublished", Value = "unpublished" }
        };

        protected string selectedTab = "live";

        public List<IndexedArticle> IndexedLiveArticles { get; set; } = [];

        protected bool IsLoadingDataGrid { get; set; } = false;

        protected bool IsSearchEnabled { get; set; } = false;

        protected string SearchString { get; set; } = string.Empty;

        public List<NewsArticleDTO> SearchListOfNewsArticles { get; set; } = new();

        public class NewsArticleSearchResponse
        {
            public List<NewsArticleDTO> Items { get; set; } = [];
        }

        protected bool isTabLoading = false;
        protected bool isTableLoading = false;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                isTabLoading = true;
                await AuthorizedPage();
                Categories = await GetNewsCategories() ?? [];
                Slots = await GetSlots();
                isTabLoading = false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync");
                throw;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                await JS.InvokeVoidAsync("initializeArticleSortable", "live-article-table", DotNetObjectReference.Create(this));

                if (shouldFocusInput && subCategoryInputRef is not null)
                {
                    shouldFocusInput = false;
                    await subCategoryInputRef.FocusAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnAfterRenderAsync");
                throw;
            }
        }

        protected async override Task OnParametersSetAsync()
        {
            try
            {
                isTabLoading = true;
                if (CategoryId > 0)
                {
                    activeIndex = 0; // Reset Index when Category is switched
                    SubCategories = Categories.Where(c => c.Id == CategoryId)?.FirstOrDefault()?.SubCategories ?? [];
                    SelectedSubcategory = SubCategories.FirstOrDefault() ?? new NewsSubCategory { Id = 1001, SubCategoryName = "Qatar" };
                    isTabLoading = false;
                    isTableLoading = true;
                    await OnTabChanged("live");
                    isTableLoading = false;
                }
                isTabLoading = false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnParametersSetAsync");
                throw;
            }
        }

        protected void NavigateToAddArticle()
        {
            Navigation.NavigateTo($"/manage/news/addarticle/category/{CategoryId}/subcategory/{SelectedSubcategory.Id}", true);
        }

        protected async Task DeleteArticle(Guid id)
        {
            try
            {
                var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
                var dialog = await DialogService.ShowAsync<DeleteArticleConfirmDialog>("", options);
                var result = await dialog.Result;
                if (!result.Canceled)
                {
                    var response = await newsService.DeleteNews(id);
                    if (response != null && response.IsSuccessStatusCode)
                    {
                        if (selectedTab == "live")
                        {
                            IndexedLiveArticles.RemoveAll(a => a.Article?.Id == id);
                        }
                        else
                        {
                            ListOfNewsArticles.RemoveAll(a => a.Id == id);
                            if (SearchListOfNewsArticles.Count > 0)
                            {
                                SearchListOfNewsArticles.RemoveAll(a => a.Id == id);
                            }
                        }
                        Snackbar.Add("Article Deleted successfully", Severity.Success);
                    }
                    else if (response?.StatusCode == HttpStatusCode.Conflict)
                    {
                        Snackbar.Add("Article cannot be Deleted since it is configured in News/Daily Slot", Severity.Error);
                    }
                    else if (response?.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Snackbar.Add("You are not Authorized to perform this action", Severity.Error);
                    }
                    else if (response?.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        Snackbar.Add("Internal API Error", Severity.Error);
                    }
                }

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DeleteArticle");
            }
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
                var apiResponse = await newsService.GetArticlesBySubCategory(categoryId, subCategoryId, 0, null, null);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var rawJson = await apiResponse.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<List<NewsArticleDTO>>(rawJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result ?? [];
                }

                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsBySubCategories");
                return [];
            }
        }


        protected async Task<List<NewsArticleDTO>> GetNewsBySubCategories(
                                                                            int categoryId,
                                                                            int subCategoryId,
                                                                            int? status = 0,
                                                                            int? page = null,
                                                                            int? pageSize = null,
                                                                            string? search = null)
        {
            try
            {
                var apiResponse = await newsService.GetArticlesBySubCategory(categoryId, subCategoryId, status, page, pageSize, search);

                if (apiResponse.IsSuccessStatusCode)
                {
                    var rawJson = await apiResponse.Content.ReadAsStringAsync();

                    var result = JsonSerializer.Deserialize<List<NewsArticleDTO>>(rawJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result ?? [];
                }

                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsBySubCategories");
                return [];
            }
        }


        protected async void LoadCategory(int categoryId, NewsSubCategory subCategory)
        {
            isTableLoading = true;
            SelectedSubcategory = subCategory;
            await OnTabChanged("live");
            isTableLoading = false;
            StateHasChanged();
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

        protected int GetCurrentSlot(NewsArticleDTO articleDTO)
        {
            var selectedCategory = articleDTO.Categories
                   .FirstOrDefault(c => c.CategoryId == CategoryId && c.SubcategoryId == SelectedSubcategory.Id);

            return selectedCategory?.SlotId ?? 0;
        }

        public string GetTimeDifferenceFromNowUtc(DateTime givenUtcTime)
        {
            try
            {
                var now = DateTime.UtcNow.ToQatarTime();
                var diff = now - givenUtcTime.ToQatarTime();

                // Check if the given time is in the future
                var isFuture = diff.TotalSeconds < 0;
                var absDiff = diff.Duration();

                if (absDiff.TotalHours >= 24)
                {
                    var days = (int)absDiff.TotalDays;
                    return isFuture ? $"in {days} day(s)" : $"{days} day(s) ago";
                }
                else if (absDiff.TotalHours >= 1)
                {
                    var hours = Math.Round(absDiff.TotalHours, 1);
                    return isFuture ? $"in {hours} hour(s)" : $"{hours} hour(s) ago";
                }
                else
                {
                    var minutes = (int)Math.Round(absDiff.TotalMinutes);
                    return isFuture ? $"in {minutes} minute(s)" : $"{minutes} minute(s) ago";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetTimeDifferenceFromNowUtc");
                return "N/A";
            }
        }

        protected async Task UpdateSubCategory()
        {
            try
            {
                if (!IsEditingSubCategoryName)
                {
                    // First click: Enter edit mode
                    if (SelectedSubcategory == null) return;

                    EditableSubCategoryName = new NewsSubCategory
                    {
                        Id = SelectedSubcategory.Id,
                        SubCategoryName = SelectedSubcategory.SubCategoryName
                    };
                    IsEditingSubCategoryName = true;
                    shouldFocusInput = true;
                    return;
                }

                if (string.IsNullOrWhiteSpace(EditableSubCategoryName.SubCategoryName) || string.IsNullOrEmpty(EditableSubCategoryName.SubCategoryName))
                {
                    Snackbar.Add("Subcategory Name is required", severity: Severity.Error);
                    return;
                }

                var response = await newsService.UpdateSubCategory(CategoryId, EditableSubCategoryName);
                if (response != null)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        Snackbar.Add("Subcategory Name Updated", severity: Severity.Success);
                        SelectedSubcategory.SubCategoryName = EditableSubCategoryName.SubCategoryName;
                        var subInList = SubCategories.FirstOrDefault(x => x.Id == EditableSubCategoryName.Id);
                        if (subInList != null)
                        {
                            subInList.SubCategoryName = EditableSubCategoryName.SubCategoryName;
                        }
                    }
                    else if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        APIError? error = JsonSerializer.Deserialize<APIError>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        Snackbar.Add(error?.Detail ?? "Subcategory not found.", Severity.Error);
                    }
                }
                IsEditingSubCategoryName = false;
                EditableSubCategoryName = new();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "UpdateSubCategory");
            }
        }

        protected async Task OnTabChanged(string newTab)
        {
            selectedTab = newTab;

            int? status = newTab switch
            {
                "live" => 1,
                "published" => 2,
                "unpublished" => 3,
                _ => null
            };

            isTableLoading = true;
            IsLoadingDataGrid = true;
            if (ListOfNewsArticles.Count > 0)
            {
                ListOfNewsArticles.Clear();
            }
            try
            {
                if (status.HasValue)
                {
                    switch (status.Value)
                    {
                        case 1:
                            if (IndexedLiveArticles.Count > 0)
                            {
                                IndexedLiveArticles.Clear();
                            }
                            ResetSearch();
                            IndexedLiveArticles = await GetLiveArticlesAsync();
                            break;
                        case 2:
                            ResetSearch();
                            ListOfNewsArticles = await GetPublishedArticlesAsync();
                            break;
                        case 3:
                            ResetSearch();
                            ListOfNewsArticles = await GetUnpublishedArticlesAsync();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"OnTabChanged:{newTab}");
            }
            finally
            {
                IsLoadingDataGrid = false;
                isTableLoading = false;
            }
        }

        protected async Task ShowGoLiveDialog(NewsArticleDTO newsArticle)
        {
            try
            {
                var parameters = new DialogParameters
                {
                    { nameof(GoLiveDialogBase.Title), "Go Live" },
                    { nameof(GoLiveDialogBase.Placeholder), "Slot Number" },
                    { nameof(GoLiveDialogBase.NewsArticle), newsArticle },
                    { nameof(GoLiveDialogBase.CategoryId), CategoryId },
                    { nameof(GoLiveDialogBase.SubCategoryId), SelectedSubcategory.Id },
                    { nameof(GoLiveDialogBase.Slots), Slots },
                };
                var options = new DialogOptions
                {
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true,
                    CloseOnEscapeKey = true
                };

                var dialog = await DialogService.ShowAsync<GoLiveDialog>("", parameters, options);
                var result = await dialog.Result;

                if (!result.Canceled)
                {
                    await OnTabChanged(selectedTab);
                    Snackbar.Add($"Article is Live now", Severity.Success);
                }

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ShowGoLiveDialog");
            }
        }

        protected async Task ShowPublishActionDialog(NewsArticleDTO newsArticle)
        {
            try
            {
                int? status = selectedTab switch
                {
                    "live" => 1,
                    "published" => 2,
                    "unpublished" => 3,
                    _ => null
                };

                var title = string.Empty;
                var successMessage = string.Empty;
                if (status == 1)
                {
                    title = "UnPublish Article";
                    successMessage = "Article UnPublished";
                }
                else
                {
                    title = status == 3 ? "Publish Article" : "UnPublish Article";
                    successMessage = status == 3 ? "Article Published" : "Article UnPublished";
                }

                var parameters = new DialogParameters
                {
                    { nameof(PublishArticleDialogBase.Title), title },
                    { nameof(PublishArticleDialogBase.NewsArticle), newsArticle },
                    { nameof(PublishArticleDialogBase.CategoryId), CategoryId },
                    { nameof(PublishArticleDialogBase.SubCategoryId), SelectedSubcategory.Id },
                    { nameof(PublishArticleDialogBase.UnPublishSlotId), Slots.FirstOrDefault(s => s.Id == 15)?.Id ?? 15 },
                    { nameof(PublishArticleDialogBase.PublishSlotId),Slots.FirstOrDefault(s => s.Id == 14)?.Id ?? 14 },
                    {nameof(PublishArticleDialogBase.SelectedTab), status }
                };
                var options = new DialogOptions
                {
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true,
                    CloseOnEscapeKey = true
                };

                var dialog = await DialogService.ShowAsync<PublishArticleDialog>("", parameters, options);
                var result = await dialog.Result;

                if (!result.Canceled)
                {
                    await OnTabChanged(selectedTab);
                    Snackbar.Add($"{successMessage}", Severity.Success);
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ShowPublishActionDialog");
            }
        }

        protected async Task<List<IndexedArticle>> GetLiveArticlesAsync()
        {
            try
            {
                var liveArticles = await GetNewsBySubCategories(CategoryId, SelectedSubcategory.Id, status: 1, 1, 1000) ?? [];

                var indexed = Enumerable.Range(1, 13)
                    .Select(slotNumber => new IndexedArticle
                    {
                        SlotNumber = slotNumber,
                        Article = liveArticles.FirstOrDefault(article =>
                            article.IsActive &&
                            article.Categories.Any(c =>
                                c.CategoryId == CategoryId &&
                                c.SubcategoryId == SelectedSubcategory.Id &&
                                c.SlotId == slotNumber))
                    })
                    .ToList();

                return indexed;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetLiveArticlesAsync");
                return [];
            }
        }

        protected async Task<List<NewsArticleDTO>> GetPublishedArticlesAsync()
        {
            try
            {
                var articles = await GetNewsBySubCategories(CategoryId, SelectedSubcategory.Id, status: 2, 1, 1000);

                return articles?
                    .Where(a => a.IsActive &&
                                a.Categories.Any(c => c.CategoryId == CategoryId &&
                                                      c.SubcategoryId == SelectedSubcategory.Id &&
                                                      c.SlotId == 14))
                    .OrderBy(a => a.Categories
                        .FirstOrDefault(c => c.CategoryId == CategoryId &&
                                             c.SubcategoryId == SelectedSubcategory.Id)?.SlotId)
                    .ToList() ?? [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetPublishedArticlesAsync");
                return [];
            }
        }

        protected async Task<List<NewsArticleDTO>> GetUnpublishedArticlesAsync()
        {
            try
            {
                var articles = await GetNewsBySubCategories(CategoryId, SelectedSubcategory.Id, status: 3, 1, 1000);

                return articles?
                    .Where(a => a.IsActive &&
                                a.Categories.Any(c => c.CategoryId == CategoryId &&
                                                      c.SubcategoryId == SelectedSubcategory.Id &&
                                                      c.SlotId == 15))
                    .OrderBy(a => a.Categories
                        .FirstOrDefault(c => c.CategoryId == CategoryId &&
                                             c.SubcategoryId == SelectedSubcategory.Id)?.SlotId)
                    .ToList() ?? [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetUnpublishedArticlesAsync");
                return [];
            }
        }

        protected async void SearchArticles()
        {
            try
            {
                int? statusTab = selectedTab switch
                {
                    "live" => 1,
                    "published" => 2,
                    "unpublished" => 3,
                    _ => null
                };
                IsSearchEnabled = true;
                IsLoadingDataGrid = true;
                if (statusTab == 1)
                {
                    await OnTabChanged("published");
                    statusTab = 2;
                }

                SearchListOfNewsArticles = await GetNewsBySubCategories(CategoryId, SelectedSubcategory.Id, status: statusTab, search: SearchString);
                IsLoadingDataGrid = false;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SearchArticles");
                IsLoadingDataGrid = false;
            }
        }

        private async Task<List<NewsArticleDTO>> SearchArticlesAsync(string searchString)
        {
            try
            {
                var response = await newsService.SearchArticles(searchString);
                if (response != null)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<NewsArticleSearchResponse>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        return result?.Items ?? new List<NewsArticleDTO>();
                    }
                    else if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        APIError? error = JsonSerializer.Deserialize<APIError>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        Snackbar.Add(error?.Detail ?? "Search Error", Severity.Error);
                    }
                    else if (response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        APIError? error = JsonSerializer.Deserialize<APIError>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        Snackbar.Add(error?.Detail ?? "Internal Server Error", Severity.Error);
                    }
                }

                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SearchArticles");
                return [];
            }
        }


        [JSInvokable]
        public async Task OnTableReordered(List<string> newOrder)
        {
            try
            {
                var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
                var dialog = await DialogService.ShowAsync<ReOrderConfirmDialog>("", options);
                var result = await dialog.Result;
                if (result is not null)
                {
                    if (result.Canceled)
                    {
                        // Reset the List
                        await ResetOrder();
                    }
                    if (!result.Canceled)
                    {
                        // Convert to integers: newOrder contains original SlotNumbers in new order
                        var reorderedSlotNumbers = newOrder.Select(int.Parse).ToList();

                        // Map of original slot numbers to their article IDs
                        var articleSlotMap = IndexedLiveArticles
                            .Where(x => x.Article != null)
                            .ToDictionary(x => x.SlotNumber, x => x.Article!.Id);

                        // Prepare 13-slot list (1 to 13)
                        var totalSlots = Enumerable.Range(1, 13).ToList();

                        // Create ArticleSlotAssignments based on reordering
                        var slotAssignments = totalSlots.Select(slotNumber =>
                        {
                            // Find the index of this slot in the reordered list
                            var newIndex = reorderedSlotNumbers.IndexOf(slotNumber);

                            // If this slot was part of the newOrder list, use its new index + 1
                            var newSlotNumber = newIndex >= 0 ? newIndex + 1 : slotNumber;

                            // Lookup the articleId for this original slot number
                            var articleId = articleSlotMap.TryGetValue(slotNumber, out var id) ? (Guid?)id : null;

                            return new ArticleSlotAssignment
                            {
                                SlotNumber = newSlotNumber,
                                ArticleId = articleId
                            };
                        })
                        .OrderBy(x => x.SlotNumber) // optional: keep the output ordered
                        .ToList();


                        var request = new ReorderRequest
                        {
                            SlotAssignments = slotAssignments,
                            CategoryId = CategoryId,
                            SubCategoryId = SelectedSubcategory.Id,
                            UserId = CurrentUserId.ToString()
                        };

                        var response = await newsService.ReOrderNews(request, CurrentUserId.ToString());
                        var content = await response.Content.ReadAsStringAsync();
                        if (response.IsSuccessStatusCode)
                        {
                            Snackbar.Add("Slot reordered successfully", Severity.Success);
                        }

                        if (!response.IsSuccessStatusCode)
                        {
                            Snackbar.Add("Failed to Reorder slots", Severity.Error);
                            Logger.LogError("Reorder API failed: {StatusCode}", response.StatusCode);
                        }
                        StateHasChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnTableReordered");
            }
        }

        protected void ResetSearch()
        {
            IsSearchEnabled = false;
            SearchString = string.Empty;
        }

        protected async Task ResetOrder()
        {
            try
            {
                await JS.InvokeVoidAsync("resetArticleTableOrder");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ResetOrder");
                throw;
            }
        }
    }
}