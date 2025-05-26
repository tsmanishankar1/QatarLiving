using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.AIPOV.Frontend.Blazor.Client.Models;
using QLN.AIPOV.Frontend.Blazor.Client.Services.Interfaces;

namespace QLN.AIPOV.Frontend.Blazor.Client.Components
{
    public partial class ChatBot
    {
        [Inject, EditorRequired] public required IChatService ChatService { get; set; } = default!;
        [Inject, EditorRequired] public required IJSRuntime JSRuntime { get; set; } = default!;
        [Inject, EditorRequired] public required ISnackbar SnackBar { get; set; }

        private List<ChatMessageModel> Messages { get; set; } = new();

        private Task SendMessageAsync(string message)
        {
            //try
            //{
            //    Messages = (await ChatService.GetMessagesAsync(message)).ToList();
            //}
            //catch (Exception ex)
            //{
            //    await JSRuntime.InvokeVoidAsync("console.log", @$"Error saving schedule. {ex.GetBaseException().Message}");
            //    SnackBar.Add("Error chatting with the bot", Severity.Error);
            //}

            return Task.CompletedTask;
        }
    }
}
