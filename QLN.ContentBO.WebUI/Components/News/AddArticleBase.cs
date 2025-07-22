using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using MudExRichTextEditor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Net;

namespace QLN.ContentBO.WebUI.Components.News
{
    public class AddArticleBase : QLComponentBase
    {
        [Inject] INewsService newsService { get; set; }
        [Inject] ILogger<AddArticleBase> Logger { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Parameter] public int? CategoryId { get; set; }
        [Parameter] public int? SubCategoryId { get; set; }

        protected NewsArticleDTO article { get; set; } = new();

        protected List<NewsCategory> Categories = [];
        protected List<Slot> Slots = [];
        protected List<string> WriterTags = [];

        protected MudExRichTextEdit Editor;

        protected ArticleCategory Category { get; set; } = new();

        protected List<ArticleCategory> TempCategoryList { get; set; } = [];

        public int MaxCategory { get; set; } = 2;

        public bool IsLoading { get; set; } = false;

        public bool IsBtnDisabled { get; set; } = false;

        protected ArticleCategory CategoryTwo { get; set; } = new();
        public bool IsAddingCategoryTwo { get; set; } = false;

        protected List<NewsSubCategory> FilteredSubCategories = [];
        protected List<NewsSubCategory> FilteredSubCategoriesTwo = [];

        protected override async Task OnInitializedAsync()
        {
            try
            {
                IsLoading = true;
                await AuthorizedPage();
                Categories = await GetNewsCategories();
                Slots = await GetSlots();
                WriterTags = await GetWriterTags();
                IsLoading = false;
            }
            catch (Exception ex)
            {
                IsLoading = false;
                Logger.LogError(ex, "OnInitializedAsync");
                throw;
            }
        }

        protected async override Task OnParametersSetAsync()
        {
            try
            {
                IsLoading = true;
                if (CategoryId > 0 || SubCategoryId > 0)
                {
                    Categories = await GetNewsCategories() ?? [];

                    Category = new()
                    {
                        CategoryId = CategoryId ?? 0,
                        SubcategoryId = SubCategoryId ?? 0,
                        SlotId = 15,
                    };


                    FilteredSubCategories = Categories
                        .FirstOrDefault(c => c.Id == CategoryId)?
                        .SubCategories ?? [];
                }
                IsLoading = false;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                IsLoading = false;
                Logger.LogError(ex, "OnParametersSetAsync");
                throw;
            }
        }

        protected void AddCategory()
        {
            IsAddingCategoryTwo = true;
        }

        protected void RemoveCategoryTwo()
        {
            CategoryTwo.CategoryId = 0;
            CategoryTwo.SubcategoryId = 0;
            CategoryTwo.SlotId = 0;
            FilteredSubCategoriesTwo = [];
            IsAddingCategoryTwo = false;
        }

        protected async Task HandleValidSubmit()
        {
            try
            {
                IsBtnDisabled = true;

                if (!IsValidCategory(Category))
                {
                    ShowError("Category and Sub Category is required");
                    return;
                }
                Category.SlotId = Category.SlotId == 0 ? 15 : Category.SlotId;

                if (IsAddingCategoryTwo)
                {
                    if (!IsValidCategory(CategoryTwo))
                    {
                        ShowError("Category and Sub Category is required");
                        return;
                    }

                    CategoryTwo.SlotId = CategoryTwo.SlotId == 0 ? 15 : CategoryTwo.SlotId;
                }

                if (IsDuplicate(Category, CategoryTwo))
                {
                    ShowError("This Category and Sub Category combination already exists");
                    return;
                }

                TempCategoryList.Add(Category);

                if (IsAddingCategoryTwo)
                {
                    TempCategoryList.Add(CategoryTwo);
                }

                article.Categories = TempCategoryList;
                if (article.Categories.Count == 0)
                {
                    ShowError("Select atleast one category");
                    return;
                }
                if (string.IsNullOrEmpty(article.CoverImageUrl))
                {
                    ShowError("Cover Image is required");
                    return;
                }
                if (string.IsNullOrEmpty(article.Content) || string.IsNullOrWhiteSpace(article.Content) || article.Content == "<p></p>" || article.Content == "<p> </p>")
                {
                    ShowError("Article Content is required");
                    return;
                }

                article.UserId = CurrentUserId.ToString();
                article.IsActive = true;

                var response = await newsService.CreateArticle(article);
                if (response != null && response.IsSuccessStatusCode)
                {
                    var parameters = new DialogParameters<ArticleDialog>
                    {
                        { x => x.ContentText, "Article Published" },
                    };

                    var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
                    await DialogService.ShowAsync<ArticleDialog>("", parameters, options);
                    ResetForm();
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Snackbar.Add("You are unauthorized to perform this action");
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Snackbar.Add("Internal API Error");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleValidSubmit");
                ResetForm();
                Snackbar.Add("Article could not be created", Severity.Error);
            }
            finally
            {
                IsBtnDisabled = false;
            }
        }

        protected void RemoveImage()
        {
            article.CoverImageUrl = null;
        }

        protected async Task HandleFilesChanged(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file != null)
                {
                    using var stream = file.OpenReadStream(5 * 1024 * 1024); // 5MB limit
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    var base64 = Convert.ToBase64String(memoryStream.ToArray());
                    article.CoverImageUrl = $"data:{file.ContentType};base64,{base64}";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleFilesChanged");
                ResetForm();
            }
        }

        protected async void Cancel()
        {
            var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = await DialogService.ShowAsync<DiscardArticleDialog>("", options);
            var result = await dialog.Result;
            if (!result.Canceled)
            {
                ResetForm();
                StateHasChanged();
            }
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

        private async Task<List<string>> GetWriterTags()
        {
            try
            {
                var apiResponse = await newsService.GetWriterTags();
                if (apiResponse.IsSuccessStatusCode)
                {
                    var tagResponse = await apiResponse.Content.ReadFromJsonAsync<TagResponse>();

                    return tagResponse?.Tags ?? [];
                }

                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetWriterTags");
                return [];
            }
        }

        protected string? GetSubCategoryName(int CategoryId, int subCategoryId)
        {
            return Categories
                .FirstOrDefault(c => c.Id == CategoryId)?
                .SubCategories
                .FirstOrDefault(sc => sc.Id == subCategoryId)?
                .SubCategoryName;
        }

        protected void ResetForm()
        {
            article = new();
            TempCategoryList = [];
            if (CategoryId is not null && SubCategoryId is not null)
            {
                TempCategoryList.Add(new()
                {
                    CategoryId = CategoryId ?? 0,
                    SubcategoryId = SubCategoryId ?? 0,
                    SlotId = 15,
                });
            }
        }

        protected async Task OnCategoryChanged(int newCategoryId)
        {
            Category.CategoryId = newCategoryId;
            Category.SubcategoryId = 0;
            Category.SlotId = 0;

            FilteredSubCategories = Categories
                .FirstOrDefault(c => c.Id == newCategoryId)?
                .SubCategories ?? [];

            await InvokeAsync(StateHasChanged);
        }

        protected async Task OnCategoryTwoChanged(int newCategoryId)
        {
            CategoryTwo.CategoryId = newCategoryId;
            CategoryTwo.SubcategoryId = 0;
            CategoryTwo.SlotId = 0;

            FilteredSubCategoriesTwo = Categories
                .FirstOrDefault(c => c.Id == newCategoryId)?
                .SubCategories ?? [];

            await InvokeAsync(StateHasChanged);
        }

        private bool IsValidCategory(ArticleCategory category)
        {
            return category.CategoryId != 0 && category.SubcategoryId != 0;
        }

        private bool IsDuplicate(ArticleCategory categoryOne, ArticleCategory categoryTwo)
        {
            if (!ReferenceEquals(categoryOne, categoryTwo) && categoryOne?.CategoryId == categoryTwo?.CategoryId && categoryOne?.SubcategoryId == categoryTwo?.SubcategoryId)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ShowError(string message)
        {
            Snackbar.Add(message, Severity.Error);
            IsBtnDisabled = false;
        }
    }
}
