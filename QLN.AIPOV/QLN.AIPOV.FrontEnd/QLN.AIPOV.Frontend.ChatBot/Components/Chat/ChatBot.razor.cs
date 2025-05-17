using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.AIPOV.Frontend.ChatBot.Models.Chat;
using QLN.AIPOV.FrontEnd.ChatBot.Services.Interfaces;

namespace QLN.AIPOV.Frontend.ChatBot.Components.Chat
{
    public partial class ChatBot
    {
        [Inject, EditorRequired] public required IChatService ChatService { get; set; } = default!;
        [Inject, EditorRequired] public required IJSRuntime JSRuntime { get; set; } = default!;
        [Inject, EditorRequired] public required ISnackbar SnackBar { get; set; }

        private List<ChatMessageModel> Messages { get; set; } = new();

        private async Task SendMessageAsync(string message)
        {
            try
            {
                var chatCompletionResponse = await ChatService.GetMessagesAsync(message);
                Messages = chatCompletionResponse.Message.Messages;
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("console.log", @$"Error saving schedule. {ex.GetBaseException().Message}");
                SnackBar.Add("Error chatting with the bot", Severity.Error);
            }
        }
    }
}
