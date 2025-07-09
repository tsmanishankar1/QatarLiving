using Microsoft.AspNetCore.Components;
using MudBlazor;
using OneOf.Types;
using QLN.Web.Shared.Contracts;

namespace QLN.Web.Shared.Components.ReportDialog
{
    public class ReportDialogBase : ComponentBase
    {
        [CascadingParameter] IMudDialogInstance MudDialog { get; set; }

        [Inject] protected ICommunityService CommunityService { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        [Parameter] public string PostId { get; set; }
        [Parameter] public string CommentId { get; set; }
        [Parameter] public string Type { get; set; }

        protected bool IsBtnDisabled { get; set; }

        protected async Task OnClickReport()
        {
            IsBtnDisabled = true;
            bool success = false;

            //var success = await CommunityService.ReportCommunityPostAsync(PostId);
            switch (Type)
            {
                case "Comment":
                    success = await CommunityService.ReportCommentAsync(PostId, CommentId);
                    break;

                //case "News":
                //    success = await CommunityService.ReportNewsAsync(PostId);
                //    break;

                default:
                    success = await CommunityService.ReportCommunityPostAsync(PostId);
                    break;
            }
            if (success)
            {
                Snackbar.Add("Reported successfully.", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add("Failed to report.", Severity.Error);
            }

            IsBtnDisabled = false;
        }

        protected void Cancel() => MudDialog.Cancel();
        protected string GetReportConfirmationText()
        {
            return Type switch
            {
                "Comment" => "Are you sure you want to report this comment?",
                "News" => "Are you sure you want to report this news item?",
                _ => "Are you sure you want to report this post?"
            };
        }
        protected string GetReportConfirmationHeaderText()
        {
            return Type switch
            {
                "Comment" => "Report Comment",
                "News" => "Report News",
                _ => "Report Post"
            };
        }
    }
}
