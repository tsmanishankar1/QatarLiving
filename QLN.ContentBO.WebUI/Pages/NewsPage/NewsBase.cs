using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;
using static QLN.ContentBO.WebUI.Components.ToggleTabs.ToggleTabs;

namespace QLN.ContentBO.WebUI.Pages.NewsPage
{
    public class NewsBase : QLComponentBase
    {
        [Inject] INewsService newsService { get; set; }
        [Inject] ILogger<NewsBase> Logger { get; set; }
        [Inject] protected NavigationManager Navigation { get; set; }
        [Parameter] public int CategoryId { get; set; }

        protected int activeIndex = 0;

        protected string searchText;

        protected string selectedType;

        public List<NewsArticleDTO> ListOfNewsArticles { get; set; }

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

        [Parameter] public EventCallback<int?> OnStatusChanged { get; set; }  // New param to send status

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (shouldFocusInput && subCategoryInputRef is not null)
            {
                shouldFocusInput = false;
                await subCategoryInputRef.FocusAsync();
            }
        }

        protected async override Task OnParametersSetAsync()
        {
            if (CategoryId > 0)
            {
                Categories = await GetNewsCategories() ?? [];
                SubCategories = Categories.Where(c => c.Id == CategoryId)?.FirstOrDefault()?.SubCategories ?? [];
                SelectedSubcategory = SubCategories.First();

                ListOfNewsArticles = (await GetNewsBySubCategories(CategoryId, SelectedSubcategory.Id))?
                                                     .Where(a => a.IsActive)
                                                     .OrderBy(a => a.Categories
                                                         .FirstOrDefault(c => c.CategoryId == CategoryId && c.SubcategoryId == SelectedSubcategory.Id)?.SlotId
                                                     )
                                                     .ToList() ?? [];

                Slots = await GetSlots();
            }
        }

        protected void NavigateToAddEvent()
        {
            Navigation.NavigateTo("/manage/news/addarticle");
        }

        protected async void DeleteArticle(Guid Id)
        {
            await DeleteNewsArticle(Id);
            ListOfNewsArticles.RemoveAll(a => a.Id == Id);
            StateHasChanged();
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

        protected async void Click_MoveItemUp(Guid Id)
        {
            try
            {
                var articleToUpdate = ListOfNewsArticles.Where(a => a.IsActive).FirstOrDefault(a =>
                    a.Id == Id &&
                    a.Categories.Any(c => c.CategoryId == CategoryId && c.SubcategoryId == SelectedSubcategory.Id)
                ) ?? new();

                var toSlot = GetCurrentSlot(articleToUpdate);

                var selectedCategory = articleToUpdate.Categories
                    .FirstOrDefault(c => c.CategoryId == CategoryId && c.SubcategoryId == SelectedSubcategory.Id);


                if (toSlot > 1)
                {
                    toSlot -= 1;
                }

                articleSlotAssignment = new()
                {
                    CategoryId = CategoryId,
                    SubCategoryId = SelectedSubcategory.Id,
                    FromSlot = selectedCategory?.SlotId ?? 0,
                    ToSlot = toSlot,
                    AuthorName = articleToUpdate.authorName ?? string.Empty,
                    UserId = articleToUpdate.UserId ?? string.Empty
                };

                var apiResponse = await newsService.ReOrderNews(articleSlotAssignment);
                if (apiResponse.IsSuccessStatusCode)
                {
                    ListOfNewsArticles = new();
                    ListOfNewsArticles = (await GetNewsBySubCategories(CategoryId, SelectedSubcategory.Id))?
                                                         .Where(a => a.IsActive)
                                                         .OrderBy(a => a.Categories
                                                             .FirstOrDefault(c => c.CategoryId == CategoryId && c.SubcategoryId == SelectedSubcategory.Id)?.SlotId
                                                         )
                                                         .ToList() ?? [];
                    Snackbar.Add("Slot Updated");
                    StateHasChanged();
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
                var articleToUpdate = ListOfNewsArticles.Where(a => a.IsActive).FirstOrDefault(a =>
                    a.Id == Id &&
                    a.Categories.Any(c => c.CategoryId == CategoryId && c.SubcategoryId == SelectedSubcategory.Id)
                ) ?? new();

                var toSlot = GetCurrentSlot(articleToUpdate);

                var selectedCategory = articleToUpdate.Categories
                    .FirstOrDefault(c => c.CategoryId == CategoryId && c.SubcategoryId == SelectedSubcategory.Id);

                if (toSlot < 13)
                {
                    toSlot += 1;
                }

                articleSlotAssignment = new()
                {
                    CategoryId = CategoryId,
                    SubCategoryId = SelectedSubcategory.Id,
                    FromSlot = selectedCategory?.SlotId ?? 0,
                    ToSlot = toSlot,
                    AuthorName = articleToUpdate.authorName ?? string.Empty,
                    UserId = articleToUpdate.UserId ?? string.Empty
                };

                var apiResponse = await newsService.ReOrderNews(articleSlotAssignment);
                if (apiResponse.IsSuccessStatusCode)
                {
                    ListOfNewsArticles = new();
                    ListOfNewsArticles = (await GetNewsBySubCategories(CategoryId, SelectedSubcategory.Id))?
                                                         .Where(a => a.IsActive)
                                                         .OrderBy(a => a.Categories
                                                             .FirstOrDefault(c => c.CategoryId == CategoryId && c.SubcategoryId == SelectedSubcategory.Id)?.SlotId
                                                         )
                                                         .ToList() ?? [];
                    Snackbar.Add("Slot Updated");
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Click_MoveItemDown");
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

        protected async void LoadCategory(int categoryId, NewsSubCategory subCategory)
        {
            SelectedSubcategory = subCategory;
            ListOfNewsArticles = (await GetNewsBySubCategories(CategoryId, SelectedSubcategory.Id))?
                                     .Where(a => a.IsActive)
                                     .OrderBy(a => a.Categories
                                         .FirstOrDefault(c => c.CategoryId == CategoryId && c.SubcategoryId == SelectedSubcategory.Id)?.SlotId
                                     )
                                     .ToList() ?? [];
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
                .SubCategoryName;
        }

        protected int GetCurrentSlot(NewsArticleDTO articleDTO)
        {
            var selectedCategory = articleDTO.Categories
                   .FirstOrDefault(c => c.CategoryId == CategoryId && c.SubcategoryId == SelectedSubcategory.Id);

            return selectedCategory?.SlotId ?? 0;
        }

        private async Task DeleteNewsArticle(Guid Id)
        {
            try
            {
                var apiResponse = await newsService.DeleteNews(Id);
                if (apiResponse.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DeleteNewsArticle");
            }
        }

        public string GetTimeDifferenceFromNowUtc(DateTime givenUtcTime)
        {
            DateTime currentUtcTime = DateTime.UtcNow;
            TimeSpan difference = currentUtcTime - givenUtcTime;

            TimeSpan absDiff = difference.Duration(); // ensures it's always positive

            if (absDiff.TotalHours >= 1)
            {
                return $"{Math.Round(absDiff.TotalHours, 2)} hours";
            }
            else
            {
                return $"{Math.Round(absDiff.TotalMinutes)} minutes";
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

                var response = await newsService.UpdateSubCategory(EditableSubCategoryName);
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
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
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

            await OnStatusChanged.InvokeAsync(status); 
        }
    }
}