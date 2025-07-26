using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using MudExRichTextEditor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Net;

namespace QLN.ContentBO.WebUI.Components.News
{
    public class EditArticleBase : QLComponentBase
    {
        [Inject] INewsService newsService { get; set; }
        [Inject] ILogger<AddArticleBase> Logger { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Parameter] public string? ArticleId { get; set; }

        protected NewsArticleDTO article { get; set; } = new();

        protected List<NewsCategory> Categories = [];
        protected List<Slot> Slots = [];
        protected List<string> WriterTags = [];

        protected MudExRichTextEdit Editor;

        protected ArticleCategory Category { get; set; } = new();

        protected List<ArticleCategory> TempCategoryList { get; set; } = [];

        public bool IsEditorReady { get; set; } = false;

        public bool IsLoading { get; set; } = false;

        public bool IsBtnDisabled { get; set; } = false;

        protected ArticleCategory CategoryTwo { get; set; } = new();
        protected List<NewsSubCategory> FilteredSubCategories = [];
        protected List<NewsSubCategory> FilteredSubCategoriesTwo = [];
        protected MudFileUpload<IBrowserFile> _fileUpload;
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
                Logger.LogError(ex, "OnInitializedAsync");
                throw;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                if (firstRender && !IsEditorReady)
                {
                    if (Editor is not null)
                    {
                        try
                        {
                            // Confirm Editor is fully ready via JSInterop
                            var html = await Editor.GetHtml();
                            if (html is not null)
                            {
                                IsEditorReady = true;
                                StateHasChanged();
                            }
                        }
                        catch (JSException jsEx)
                        {
                            Logger.LogWarning(jsEx, "MudEx Editor JS not ready yet");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "OnAfterRenderAsync");
            }
        }

        protected async override Task OnParametersSetAsync()
        {
            try
            {
                IsLoading = true;
                await Task.Delay(3000);
                await LoadArticle();
                IsLoading = false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnParametersSetAsync");
            }
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

                // Assign Category to article.Categories and add CategoryTwo if it has value
                article.Categories = [Category];
                if (IsValidOptionalCategory(CategoryTwo))
                {
                    CategoryTwo.SlotId = CategoryTwo.SlotId == 0 ? 15 : CategoryTwo.SlotId;

                    if (IsDuplicate(Category, CategoryTwo))
                    {
                        ShowError("This Category and Sub Category combination already exists");
                        // Reset article.Categories
                        article.Categories = [];
                        return;
                    }

                    article.Categories.Add(CategoryTwo);
                }
                else
                {
                    ShowError("Optional Category and Sub Category is required");
                    article.Categories = [];
                    return;
                }

                if (article.Categories.Count == 0)
                {
                    Snackbar.Add("Select atleast one category", severity: Severity.Error);
                    IsBtnDisabled = false;
                    return;
                }
                if (string.IsNullOrEmpty(article.CoverImageUrl))
                {
                    Snackbar.Add("Image is required", severity: Severity.Error);
                    IsBtnDisabled = false;
                    return;
                }
                if (string.IsNullOrEmpty(article.Content) || string.IsNullOrWhiteSpace(article.Content) || article.Content == "<p></p>" || article.Content == "<p> </p>")
                {
                    Snackbar.Add("Article Content is required", severity: Severity.Error);
                    IsBtnDisabled = false;
                    return;
                }

                article.UserId = CurrentUserId.ToString();
                article.authorName = CurrentUserName;
                var response = await newsService.UpdateArticle(article);
                if (response != null && response.IsSuccessStatusCode)
                {
                    var parameters = new DialogParameters<ArticleDialog>
                            {
                                { x => x.ContentText, "Article Updated" },
                            };

                    var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
                    await DialogService.ShowAsync<ArticleDialog>("", parameters, options);
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Snackbar.Add("You are unauthorized to perform this action", Severity.Error);
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Snackbar.Add("Internal API Error", Severity.Error);
                }
                IsBtnDisabled = false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleValidSubmit");
                Snackbar.Add("Edit Article Failed", Severity.Error);
                IsBtnDisabled = false;
            }
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
                _fileUpload?.ResetValidation();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleFilesChanged");
            }
        }

        protected async void Cancel()
        {
            var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = await DialogService.ShowAsync<DiscardArticleDialog>("", options);
            var result = await dialog.Result;
            if (!result.Canceled)
            {
                // When Changes are discarded Load Article
                await LoadArticle();
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

        private async Task<NewsArticleDTO> GetArticleById(Guid articleId)
        {
            try
            {
                var apiResponse = await newsService.GetArticleById(articleId);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var result = await apiResponse.Content.ReadFromJsonAsync<NewsArticleDTO>() ?? new();

                    return result;
                }

                return new();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetArticleById");
                return new();
            }
        }

        protected async void EditImage()
        {
            await _fileUpload.OpenFilePickerAsync();
        }

        protected void RemoveImage()
        {
            article.CoverImageUrl = null;
            _fileUpload?.ResetValidation();
        }

        private async Task LoadArticle()
        {
            try
            {
                if (!Guid.TryParse(ArticleId, out var parsedArticleId))
                {
                    Snackbar.Add("Invalid article ID", Severity.Error);
                    return;
                }

                article = await GetArticleById(parsedArticleId);

                if (article?.Id == Guid.Empty)
                {
                    Snackbar.Add("Article not found", Severity.Error);
                    NavManager.NavigateTo("/", true);
                    return;
                }

                TempCategoryList = article?.Categories ?? [];

                // First value of the TempCategoryList should be the primary category
                if (TempCategoryList.Count > 0)
                {
                    Category = TempCategoryList[0];
                    FilteredSubCategories = Categories
                        .FirstOrDefault(c => c.Id == Category.CategoryId)?
                        .SubCategories ?? [];
                }
                else
                {
                    Category = new ArticleCategory();
                    FilteredSubCategories = [];
                }

                // Second value of the TempCategoryList should be the secondary category
                if (TempCategoryList.Count > 1)
                {
                    CategoryTwo = TempCategoryList[1];
                    FilteredSubCategoriesTwo = Categories
                        .FirstOrDefault(c => c.Id == CategoryTwo.CategoryId)?
                        .SubCategories ?? [];
                }
                else
                {
                    CategoryTwo = new ArticleCategory();
                    FilteredSubCategoriesTwo = [];
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadArticle");
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

        private bool IsValidOptionalCategory(ArticleCategory category)
        {
            if (category.CategoryId > 0 && category.SubcategoryId == 0)
            {
                return false;
            }
            else if (category.CategoryId > 0 && category.SubcategoryId > 0)
            {
                return true;
            }

            return false;
        }
    }
}
