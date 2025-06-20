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
        protected V2ContentNewsDto article { get; set; } = new();

        protected List<string> Categories = [];
        protected List<string> Subcategories = [];
        protected List<string> Slots = [];
        protected List<string> Writers = [];

        protected MudExRichTextEdit Editor;

        protected override async Task OnInitializedAsync()
        {

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
    }
}
