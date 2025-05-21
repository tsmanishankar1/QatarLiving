using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.AIPOV.Frontend.ChatBot.Models.Chat;

namespace QLN.AIPOV.Frontend.ChatBot.Components.Chat
{
    public partial class ConversationDialog
    {
        [Parameter] public List<JobDescription> JobDescriptions { get; set; } = new();

        [CascadingParameter] IMudDialogInstance? MudDialog { get; set; }

        private JobDescription? SelectedJobDescription { get; set; }

        private void Close() => MudDialog?.Cancel();

        private void SelectDescription()
        {
            if (SelectedJobDescription != null)
                MudDialog?.Close(DialogResult.Ok(SelectedJobDescription));
        }

        private void OnDescriptionSelected(JobDescription selectedDescription)
        {
            SelectedJobDescription = selectedDescription;
        }
    }
}