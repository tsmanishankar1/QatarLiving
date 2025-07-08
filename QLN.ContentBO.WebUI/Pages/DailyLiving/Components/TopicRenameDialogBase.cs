using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Net;

namespace QLN.ContentBO.WebUI.Pages.DailyLiving.Components
{
    public class TopicRenameDialogBase : ComponentBase
    {
        public string Title { get; set; } = "Topic Rename";
        protected string? _errorText;
        [Parameter] public string ButtonTitle { get; set; } = "Article Action";
        [Parameter] public bool ShowEditField { get; set; }
        [Parameter] public string TopicName { get; set; }
        [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public EventCallback<string> OnConfirmed { get; set; }
        public void Cancel() => MudDialog.Cancel();
         public async Task Confirm()
        {
            if (string.IsNullOrWhiteSpace(TopicName))
            {
                _errorText = "Topic name is required.";
                return;
            }
            _errorText = null;
            if (OnConfirmed.HasDelegate)
                await OnConfirmed.InvokeAsync(TopicName); 
            MudDialog.Close(DialogResult.Ok(true));
        }
    }
}
