using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;

namespace QLN.Web.Shared.Pages.Content.Community
{
    public class AddPostDialogBase : ComponentBase
    {
        [Inject] protected IPostDialogService PostDialogService { get; set; }
        [Inject] protected ICommunityService CommunityService { get; set; }
        [Inject] protected ILogger<AddPostDialogBase> Logger { get; set; }

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; }

        [Inject] protected ISnackbar Snackbar { get; set; }
        [Parameter] public EventCallback<Dictionary<string, object>> OnSearchCompleted { get; set; }
        [Parameter] public EventCallback<string> OnCategoryChanged { get; set; }



        protected void Submit() => MudDialog.Close(DialogResult.Ok(true));

        protected void Cancel() => MudDialog.Cancel();
        protected string selectedCategory;
        protected bool loading = false;

        protected string SelectedCategoryId { get; set; }

        protected List<SelectOption> CategorySelectOptions { get; set; }

        protected override async Task OnInitializedAsync()
        {

            try
            {
                CategorySelectOptions = await CommunityService.GetForumCategoriesAsync();

            }
            catch (Exception ex)
            {
                Snackbar.Add("Failed to load categories", Severity.Error);
                Logger.LogInformation(ex.Message);
                CategorySelectOptions = new List<SelectOption>();
            }
        }


        protected void OnCategoryChange(string newId)
        {
            SelectedCategoryId = newId;
            OnCategoryChanged.InvokeAsync(newId);
        }

        protected async Task RedirectToPostPage()
        {

            if (!string.IsNullOrEmpty(SelectedCategoryId))
            {
                var success = await PostDialogService.PostSelectedCategoryAsync(SelectedCategoryId);

                //if (success)
                //{
                //    Snackbar.Add("Post was successfully.", Severity.Success);
                //}
                //else
                //{
                //    Snackbar.Add("Failed to post the selected category.", Severity.Error);
                //}
            }
            else
            {
                Snackbar.Add("Please select a category before continuing.", Severity.Warning);
            }
        }


    }
}