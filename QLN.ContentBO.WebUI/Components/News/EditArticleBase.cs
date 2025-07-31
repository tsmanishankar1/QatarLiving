using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using PSC.Blazor.Components.MarkdownEditor;
using PSC.Blazor.Components.MarkdownEditor.EventsArgs;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Net;
using System.Text.Json;

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

        protected ArticleCategory Category { get; set; } = new();

        protected List<ArticleCategory> TempCategoryList { get; set; } = [];

        public bool IsEditorReady { get; set; } = false;

        public bool IsLoading { get; set; } = false;

        public bool IsBtnDisabled { get; set; } = false;

        protected ArticleCategory CategoryTwo { get; set; } = new();
        protected List<NewsSubCategory> FilteredSubCategories = [];
        protected List<NewsSubCategory> FilteredSubCategoriesTwo = [];
        protected MudFileUpload<IBrowserFile> _fileUpload;


        // Custom Markdown Editor Properties
        protected MarkdownEditor MarkdownEditorRef;
        protected MudFileUpload<IBrowserFile> _markdownfileUploadRef;
        protected string UploadImageButtonName { get; set; } = "uploadImage";
        protected string BlobContainerName { get; set; } = "content-images";

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
                    using var stream = file.OpenReadStream(2 * 1024 * 1024); // 2MB limit
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    var base64 = Convert.ToBase64String(memoryStream.ToArray());
                    article.CoverImageUrl = $"data:{file.ContentType};base64,{base64}";
                    _fileUpload?.ResetValidation();
                }
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

        #region Custom Markdown Editor

        protected async Task<FileUploadResponse> FileUploadAsync(FileUploadModel fileUploadData)
        {
            try
            {
                var response = await FileUploadService.UploadFileAsync(fileUploadData);
                var jsonString = await response.Content.ReadAsStringAsync();
                if (response != null && response.IsSuccessStatusCode)
                {
                    FileUploadResponse? result = JsonSerializer.Deserialize<FileUploadResponse>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result ?? new();
                }
                else if (response?.StatusCode == HttpStatusCode.BadRequest)
                {
                    Snackbar.Add($"Bad Request: {jsonString}", Severity.Error);
                }
                else if (response?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Snackbar.Add("You are unauthorized to perform this action", Severity.Error);
                }
                else if (response?.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Snackbar.Add("Internal API Error", Severity.Error);
                }

                return new();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "FileUploadAsync");
                return new();
            }
        }

        protected void TriggerCustomImageUpload()
        {
            _markdownfileUploadRef.OpenFilePickerAsync();
        }

        protected Task OnCustomButtonClicked(MarkdownButtonEventArgs eventArgs)
        {
            if (eventArgs.Name is not null)
            {
                if (eventArgs.Name == UploadImageButtonName)
                {
                    TriggerCustomImageUpload();
                }

            }
            return Task.CompletedTask;
        }

        protected async Task UploadFile(string base64)
        {
            try
            {
                var fileUploadData = new FileUploadModel
                {
                    Container = BlobContainerName,
                    File = base64
                };

                var fileUploadResponse = await FileUploadAsync(fileUploadData);
                if (fileUploadResponse?.IsSuccess == true)
                {
                    var imageMarkdown = $"\n![image-{fileUploadResponse.FileName}]({fileUploadResponse.FileUrl})";
                    article.Content += imageMarkdown;
                    await MarkdownEditorRef!.SetValueAsync(article.Content);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "UploadFile");
            }
        }

        protected async Task HandleMarkdownFilesChanged(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file != null)
                {
                    using var stream = file.OpenReadStream(2 * 1024 * 1024); // 2MB limit
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    var base64 = Convert.ToBase64String(memoryStream.ToArray());
                    var uploadedImageBase64 = $"data:{file.ContentType};base64,{base64}";
                    if (!string.IsNullOrWhiteSpace(uploadedImageBase64))
                    {
                        await UploadFile(uploadedImageBase64);
                    }
                    _markdownfileUploadRef?.ResetValidation();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleMarkdownFilesChanged");
            }
        }

        #endregion
    }
}
