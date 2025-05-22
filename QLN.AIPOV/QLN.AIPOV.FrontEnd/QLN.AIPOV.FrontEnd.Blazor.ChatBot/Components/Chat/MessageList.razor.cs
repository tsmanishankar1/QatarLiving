using Microsoft.AspNetCore.Components;
using QLN.AIPOV.FrontEnd.Blazor.ChatBot.Models;

namespace QLN.AIPOV.FrontEnd.Blazor.ChatBot.Components.Chat
{
    public partial class MessageList
    {
        [Parameter]
        public IEnumerable<ChatMessageModel>? Messages { get; set; }

        private string GetRoleColor(string role)
        {
            return role switch
            {
                "User" => "#E3F2FD",      // Light blue
                "Bot" => "#F3E5F5",       // Light purple
                _ => "#FFFFFF"            // Default white
            };
        }
    }
}
