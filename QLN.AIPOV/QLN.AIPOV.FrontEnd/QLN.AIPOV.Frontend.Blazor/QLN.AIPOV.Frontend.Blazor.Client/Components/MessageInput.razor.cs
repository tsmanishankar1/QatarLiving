using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace QLN.AIPOV.Frontend.Blazor.Client.Components
{
    public partial class MessageInput
    {
        [Parameter]
        public EventCallback<string> OnSend { get; set; }

        private string Message { get; set; } = string.Empty;

        private async Task Send()
        {
            if (!string.IsNullOrWhiteSpace(Message))
            {
                await OnSend.InvokeAsync(Message);
                Message = string.Empty;
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
