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
                    TempCategoryList.Add(new()
                    {
                        CategoryId = CategoryId ?? 0,
                        SubcategoryId = SubCategoryId ?? 0,
                        SlotId = 15,
                    });
                }
                IsLoading = false;
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
            if (Category.CategoryId == 0 || Category.SubcategoryId == 0)
            {
                Snackbar.Add("Category and Sub Category is required", Severity.Error);
                return;
            }
            if (TempCategoryList.Count >= MaxCategory)
            {
                Snackbar.Add("Maximum of 2 Category and Sub Category combinations are allowed", Severity.Error);
                Category = new();
                return;
            }
            if (TempCategoryList.Any(x => x.CategoryId == Category.CategoryId && x.SubcategoryId == Category.SubcategoryId))
            {
                Snackbar.Add("This Category and Sub Category combination already exists", Severity.Error);
                return;
            }
            Category.SlotId = Category.SlotId == 0 ? 15 : Category.SlotId; // Defaults UnPublished.
            TempCategoryList.Add(Category);
            Category = new();
        }

        protected void RemoveCategory(ArticleCategory articleCategory)
        {
            if (TempCategoryList.Count > 0)
            {
                TempCategoryList.Remove(articleCategory);
            }
        }

        protected async Task HandleValidSubmit()
        {
            try
            {
                IsBtnDisabled = true;
                article.Categories = TempCategoryList;
                if (article.Categories.Count == 0)
                {
                    Snackbar.Add("Select atleast one category", severity: Severity.Error);
                    return;
                }
                if (string.IsNullOrEmpty(article.CoverImageUrl))
                {
                    Snackbar.Add("Image is required", severity: Severity.Error);
                    return;
                }
                if (string.IsNullOrEmpty(article.Content) || string.IsNullOrWhiteSpace(article.Content) || article.Content == "<p></p>" || article.Content == "<p> </p>")
                {
                    Snackbar.Add("Article Content is required", severity: Severity.Error);
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
                IsBtnDisabled = false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleValidSubmit");
                ResetForm();
                Snackbar.Add("Article could not be created", Severity.Error);
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
            if (CategoryId != 0 && SubCategoryId != 0)
            {
                TempCategoryList.Add(new()
                {
                    CategoryId = CategoryId ?? 0,
                    SubcategoryId = SubCategoryId ?? 0,
                    SlotId = 15,
                });
            }
        }
    }
}
