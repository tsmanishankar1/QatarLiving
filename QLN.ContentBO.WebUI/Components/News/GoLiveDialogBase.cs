using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Net;

namespace QLN.ContentBO.WebUI.Components.News
{
    public class GoLiveDialogBase : QLComponentBase
    {
        [Inject] INewsService newsService { get; set; }
        [Inject] ILogger<GoLiveDialogBase> Logger { get; set; }
        [Parameter] public string Title { get; set; } = "Go Live";
        
        [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
        
        [Parameter] public string Placeholder { get; set; } = "Slot Number";

        [Parameter]
        public NewsArticleDTO NewsArticle { get; set; } = new();

        [Parameter]
        public int CategoryId { get; set; } = new();

        [Parameter]
        public int SubCategoryId { get; set; } = new();

        [Parameter]
        public List<Slot> Slots { get; set; } = [];

        public Slot SelectedSlot { get; set; } = new();

        public bool IsBtnDisabled { get; set; } = false;

        protected async Task HandleValidSubmit()
        {
            try
            {
                IsBtnDisabled = true;
                
                if (SelectedSlot.Id == 0)
                {
                    Snackbar.Add("Slot is required", severity: Severity.Error);
                    return;
                }

                var targetCategory = NewsArticle.Categories.FirstOrDefault(c =>
                    c.CategoryId == CategoryId &&
                    c.SubcategoryId == SubCategoryId);

                if (targetCategory != null)
                {
                    targetCategory.SlotId = SelectedSlot.Id;
                }
                else
                {
                    Snackbar.Add("Matching category/subcategory not found in the article.", Severity.Normal);
                    return;
                }

                var response = await newsService.UpdateArticle(NewsArticle);
                if (response != null && response.IsSuccessStatusCode)
                {
                    Snackbar.Add("Go Live Slot Updated");
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
            }
        }


        public void Cancel() => MudDialog.Cancel();
    }
}
