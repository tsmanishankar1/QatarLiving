using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using PSC.Blazor.Components.MarkdownEditor;
using PSC.Blazor.Components.MarkdownEditor.EventsArgs;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services;
using System.Net;
using System.Text.Json;

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
        protected List<Writertag> WriterTags = [];

        protected ArticleCategory Category { get; set; } = new();

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
                    StateHasChanged();
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
                StateHasChanged();
            }
            finally
            {
                IsBtnDisabled = false;
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

        private async Task<List<Writertag>> GetWriterTags()
        {
            try
            {
                var apiResponse = await newsService.GetWriterTags();
                if (apiResponse.IsSuccessStatusCode)
                {
                    var writerTagResponse = await apiResponse.Content.ReadFromJsonAsync<List<Writertag>>();

                    return writerTagResponse ?? [];
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
            Category.CategoryId = 0;
            Category.SubcategoryId = 0;
            Category.SlotId = 0;
            FilteredSubCategories = [];
            CategoryTwo.CategoryId = 0;
            CategoryTwo.SubcategoryId = 0;
            CategoryTwo.SlotId = 0;
            FilteredSubCategoriesTwo = [];
            if (CategoryId is not null && SubCategoryId is not null)
            {
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
