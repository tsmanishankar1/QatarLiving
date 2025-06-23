using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudExRichTextEditor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Components.News
{
    public class AddArticleBase : ComponentBase
    {
        [Inject] INewsService newsService { get; set; }
        [Inject] ILogger<AddArticleBase> Logger { get; set; }
        [Inject] IJSRuntime JS { get; set; }
        protected NewsArticleDTO article { get; set; } = new();

        protected List<NewsCategory> Categories = [];
        protected List<Slot> Slots = [];
        protected List<string> WriterTags = [];

        protected MudExRichTextEdit Editor;

        protected override async Task OnInitializedAsync()
        {
            Categories = await GetNewsCategories();
            Slots = await GetSlots();
            WriterTags = await GetWriterTags();
        }

        protected async Task HandleValidSubmit()
        {
            try
            {
                var response = await newsService.CreateArticle(article);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleValidSubmit");
            }
        }

        protected void HandleCoverImageChange(InputFileChangeEventArgs e)
        {

        }

        protected void HandleInlineImagesChange(InputFileChangeEventArgs e)
        {

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
    }
}
