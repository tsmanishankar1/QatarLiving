using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace QLN.AIPOV.Frontend.ChatBot.Components.Chat
{
    public partial class MessageInput
    {
        [Parameter]
        public EventCallback<string> OnSend { get; set; }

        [Parameter]
        public bool DisableChatInput { get; set; }

        private string Message { get; set; } = string.Empty;

        private async Task Send()
        {
            if (!string.IsNullOrWhiteSpace(Message))
            {
                var message = Message;
                Message = string.Empty;
                StateHasChanged();
                await OnSend.InvokeAsync(message);
            }
        }

        private async Task HandleKeyDown(KeyboardEventArgs args)
        {
            if (args.Key == "Enter" && !string.IsNullOrWhiteSpace(Message))
            {
                await Send();
            }
        }
    }
}
