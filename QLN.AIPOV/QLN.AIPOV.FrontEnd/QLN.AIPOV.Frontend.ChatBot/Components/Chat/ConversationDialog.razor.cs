using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.AIPOV.Frontend.ChatBot.Models.Chat;

namespace QLN.AIPOV.Frontend.ChatBot.Components.Chat
{
    public partial class ConversationDialog
    {
        [Parameter] public List<ChatMessageModel> Messages { get; set; } = new();

        [CascadingParameter]
        IMudDialogInstance? MudDialog { get; set; }

        private string? Description { get; set; }

        private void Close() => MudDialog?.Cancel();

        private void SelectDescription()
        {
            if (!string.IsNullOrEmpty(Description))
                MudDialog?.Close(DialogResult.Ok(Description));
        }

        private void OnDescriptionSelected(ChatMessageModel selectedDescription)
        {
            Description = selectedDescription.Content;
        }
    }
}
