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
        [Inject, EditorRequired] public required IDialogService DialogService { get; set; }

        private List<ChatMessageModel> Messages { get; set; } = new();
        private string Description { get; set; } = string.Empty;
        private bool _isLoading = false;

        private async Task SendMessageAsync(string message)
        {
            try
            {
                _isLoading = true;

                Messages.Add(new ChatMessageModel
                {
                    Content = message,
                    Role = "user"
                });

                var chatCompletionResponse = await ChatService.GetMessagesAsync(message);
                Messages.AddRange(chatCompletionResponse.Message.Messages.Where(m => m.Role != "user"));

                await ShowConversationDialogAsync();
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("console.log",
                    @$"Error saving schedule. {ex.GetBaseException().Message}");
                SnackBar.Add("Error chatting with the bot", Severity.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task ShowConversationDialogAsync()
        {
            var parameters = new DialogParameters<ConversationDialog> { { x => x.Messages, Messages } };
            var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true, BackgroundClass = "dialog-blur" };

            var dialog = await DialogService.ShowAsync<ConversationDialog>("Conversation", parameters, options);
            var result = await dialog.Result;
            if (result is { Data: not null, Canceled: false } && !string.IsNullOrEmpty(result.Data.ToString()))
            {
                Description = result.Data.ToString() ?? string.Empty;
            }
        }

        private async Task CopyToClipboard(string text)
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
            SnackBar.Add("Description copied to clipboard", Severity.Success);
        }


    }
}
