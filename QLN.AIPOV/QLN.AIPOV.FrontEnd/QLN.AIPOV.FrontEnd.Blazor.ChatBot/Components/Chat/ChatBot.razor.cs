using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.AIPOV.FrontEnd.Blazor.ChatBot.Models;
using QLN.AIPOV.FrontEnd.Blazor.ChatBot.Services.Interfaces;

namespace QLN.AIPOV.FrontEnd.Blazor.ChatBot.Components.Chat
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
                Messages = (await ChatService.GetMessagesAsync(message)).ToList();
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("console.log", @$"Error saving schedule. {ex.GetBaseException().Message}");
                SnackBar.Add("Error chatting with the bot", Severity.Error);
            }
        }
    }
}
