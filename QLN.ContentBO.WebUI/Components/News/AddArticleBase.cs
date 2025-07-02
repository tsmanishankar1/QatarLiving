using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
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
        [Inject] IJSRuntime JS { get; set; }
        protected NewsArticleDTO article { get; set; } = new();

        protected List<NewsCategory> Categories = [];
        protected List<Slot> Slots = [];
        protected List<string> WriterTags = [];

        protected MudExRichTextEdit Editor;

        protected ArticleCategory Category { get; set; } = new();

        protected List<ArticleCategory> TempCategoryList { get; set; } = [];

        protected List<string> writerTags =
            [
                    "Qatar Living",
                    "Everything Qatar",
                    "FIFA Arab Cup",
                    "QL Exclusive",
                    "Advice & Help"
            ];


        protected override async Task OnInitializedAsync()
        {
            Categories = await GetNewsCategories();
            Slots = await GetSlots();
            WriterTags = await GetWriterTags();
        }

        protected void AddCategory()
        {
            TempCategoryList.Add(Category);
            Category = new();
        }

        protected void RemoveCategory(ArticleCategory articleCategory)
        {
            if (TempCategoryList.Count > 0)
            {
                TempCategoryList.Remove(articleCategory);
                Category = new();
            }
        }

        protected async Task HandleValidSubmit()
        {
            try
            {
                article.Categories = TempCategoryList;
                var response = await newsService.CreateArticle(article);
                if (response != null && response.IsSuccessStatusCode)
                {
                    Snackbar.Add("Article Added");
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Snackbar.Add("You are unauthorized to perform this action");
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleValidSubmit");
            }
        }

        protected async Task HandleFilesChanged(InputFileChangeEventArgs e)
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

        protected async Task TriggerCoverUpload()
        {
            await JS.InvokeVoidAsync("document.getElementById", "cover-upload").AsTask();
        }

        protected async Task TriggerInlineImageUpload()
        {
            await JS.InvokeVoidAsync("document.getElementById", "inline-upload").AsTask();
        }

        protected void Cancel()
        {

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
                    return await apiResponse.Content.ReadFromJsonAsync<List<string>>() ?? [];
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
                .CategoryName;
        }
    }
}
