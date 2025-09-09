using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using MudBlazor.Extensions.Helper;

namespace QLN.ContentBO.WebUI.Pages.Services.EditService
{
    public class CommentDialogBase : ComponentBase
    {
        [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; } = default!;
        [Parameter]
        public BulkModerationAction ActionType { get; set; }
        protected string Comments { get; set; } = string.Empty;

        protected void Save()
        {
            MudDialog.Close(DialogResult.Ok(Comments));
        }
        protected void Skip()
        {
            MudDialog.Cancel();
        }
        protected string GetDialogTitle()
        {
            return ActionType switch
            {
                BulkModerationAction.Feature => "Add comments for featuring this Ad",
                BulkModerationAction.Promote => "Add comments for promoting this Ad",
                BulkModerationAction.NeedChanges => "Enter the Reason for Requesting Changes",
                _ => "Add Comments"
            };
        }
        protected string GetPlaceholder()
        {
            return ActionType switch
            {
                BulkModerationAction.Feature => "Enter your comments for featuring this Ad...",
                BulkModerationAction.Promote => "Enter your comments for promoting this Ad...",
                BulkModerationAction.NeedChanges => "Enter your reason for requesting changes...",
                _ => "Enter your comments here..."
            };
        }
    }
}