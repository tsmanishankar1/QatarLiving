using Microsoft.AspNetCore.Components;
using QLN.AIPOV.Frontend.ChatBot.Models.Chat;

namespace QLN.AIPOV.Frontend.ChatBot.Components.Chat
{
    public partial class MessageList
    {
        [Parameter]
        public EventCallback<ChatMessageModel> SelectedValueChanged { get; set; }

        [Parameter]
        public IEnumerable<ChatMessageModel>? Messages { get; set; }

        public ChatMessageModel? SelectedValue { get; set; }

        private string GetRoleColor(string role)
        {
            return role switch
            {
                "user" => "#E3F2FD",      // Light blue
                "assistant" => "#F3E5F5",       // Light purple
                _ => "#FFFFFF"            // Default white
            };
        }

        private string FormatContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            // Replace actual newlines AND escaped newlines with proper HTML line breaks
            var formatted = content
                .Replace("\r\n", "<br>")  // Windows newlines
                .Replace("\n", "<br>")     // Unix newlines
                .Replace("\\n\\n", "<br><br>")  // Escaped double newlines  
                .Replace("\\n", "<br>");        // Escaped single newlines

            // Handle Markdown-like syntax
            // Bold text
            formatted = System.Text.RegularExpressions.Regex.Replace(
                formatted,
                @"\*\*([^*]+)\*\*",
                "<strong>$1</strong>");

            // Tables (rest of your table code remains the same)
            if (!formatted.Contains("|")) return formatted;

            // Rest of table formatting logic...
            var lines = formatted.Split("<br>");

            return formatted;
        }

        private async Task OnSelectedValueChanged(ChatMessageModel? message)
        {
            if (message == null) return;
            SelectedValue = message;
            await SelectedValueChanged.InvokeAsync(message);
        }
    }
}
