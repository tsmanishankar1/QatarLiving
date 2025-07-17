using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Net;

namespace QLN.ContentBO.WebUI.Components.News
{
    public class PublishArticleDialogBase: QLComponentBase
    {
        [Inject] INewsService newsService { get; set; }
        [Inject] ILogger<PublishArticleDialogBase> Logger { get; set; }
        [Parameter] public string Title { get; set; } = "Article Action";

        [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public NewsArticleDTO NewsArticle { get; set; } = new();

        [Parameter]
        public int CategoryId { get; set; } = new();

        [Parameter]
        public int SubCategoryId { get; set; } = new();

        [Parameter]
        public int UnPublishSlotId { get; set; } = new();

        [Parameter]
        public int PublishSlotId { get; set; } = new();

        [Parameter]
        public int SelectedTab { get; set; }

        public bool IsBtnDisabled { get; set; } = false;

        protected async Task OnClickPublish()
        {
            try
            {
                IsBtnDisabled = true;

                var targetCategory = NewsArticle.Categories.FirstOrDefault(c =>
                    c.CategoryId == CategoryId &&
                    c.SubcategoryId == SubCategoryId);

                if (targetCategory != null)
                {
                    targetCategory.SlotId = PublishSlotId;
                }
                else
                {
                    Snackbar.Add("Error Occured", Severity.Normal);
                    return;
                }

                var response = await newsService.UpdateArticle(NewsArticle);
                if (response != null && response.IsSuccessStatusCode)
                {
                    MudDialog.Close();
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
                Logger.LogError(ex, "OnClickPublish");
            }
        }

        protected async Task OnClickUnPublish()
        {
            try
            {
                IsBtnDisabled = true;

                var targetCategory = NewsArticle.Categories.FirstOrDefault(c =>
                    c.CategoryId == CategoryId &&
                    c.SubcategoryId == SubCategoryId);

                if (targetCategory != null)
                {
                    targetCategory.SlotId = UnPublishSlotId;
                }
                else
                {
                    Snackbar.Add("Error Occured", Severity.Normal);
                    return;
                }

                var response = await newsService.UpdateArticle(NewsArticle);
                if (response != null && response.IsSuccessStatusCode)
                {
                    MudDialog.Close();
                }
                else if (response?.StatusCode == HttpStatusCode.Conflict)
                {
                    Snackbar.Add("Article cannot be UnPublished since it is configured in Daily Top Section or Daily Topics", Severity.Error);
                }
                else if (response?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Snackbar.Add("You are unauthorized to perform this action");
                }
                else if (response?.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Snackbar.Add("Internal API Error");
                }
                IsBtnDisabled = false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnClickUnPublish");
            }
        }

        public void Cancel() => MudDialog.Cancel();
    }
}
