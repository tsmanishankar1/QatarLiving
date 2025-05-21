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

        private List<JobDescription> JobDescriptions { get; set; } = new();

        private string Description { get; set; } = string.Empty;

        private JobDescription? SelectedJobDescription { get; set; }

        private bool _isLoading = false;

        private async Task SendMessageAsync(string message)
        {
            try
            {
                _isLoading = true;

                var chatCompletionResponse = await ChatService.GetMessagesAsync(message);

                if (chatCompletionResponse.Message is { Descriptions: not null })
                {
                    JobDescriptions = chatCompletionResponse.Message.Descriptions;
                }

                await ShowConversationDialogAsync();
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("console.log",
                    @$"Error chatting with the bot. {ex.GetBaseException().Message}");
                SnackBar.Add("Error chatting with the bot", Severity.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task ShowConversationDialogAsync()
        {
            var parameters = new DialogParameters<ConversationDialog> { { x => x.JobDescriptions, JobDescriptions } };
            var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true, BackgroundClass = "dialog-blur" };

            var dialog = await DialogService.ShowAsync<ConversationDialog>("Conversation", parameters, options);
            var result = await dialog.Result;
            if (result is { Data: not null, Canceled: false })
            {
                if (result.Data is JobDescription jobDescription)
                {
                    // Store both the full job description object and the text representation
                    SelectedJobDescription = jobDescription;
                    Description = jobDescription.JobDutiesAndResponsibilities ?? string.Empty;
                }
                else if (result.Data is string textDescription)
                {
                    // Fallback to string representation if that's what was returned
                    Description = textDescription;
                    SelectedJobDescription = null;
                }
            }
        }

        private async Task CopyToClipboard(string text)
        {
            // If we have a selected job description, format it completely
            if (SelectedJobDescription != null)
            {
                var formattedDescription = FormatJobDescriptionForCopy(SelectedJobDescription);
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", formattedDescription);
                SnackBar.Add("Complete job description copied to clipboard", Severity.Success);
            }
            else
            {
                // Fallback to copying the text directly if no structured job description is available
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
                SnackBar.Add("Description copied to clipboard", Severity.Success);
            }
        }

        private string FormatJobDescriptionForCopy(JobDescription jobDescription)
        {
            var sb = new System.Text.StringBuilder();

            // Job Title
            if (!string.IsNullOrEmpty(jobDescription.JobTitle))
            {
                sb.AppendLine($"JOB TITLE");
                sb.AppendLine($"{jobDescription.JobTitle}");
                sb.AppendLine();
            }

            // Job Purpose
            if (!string.IsNullOrEmpty(jobDescription.JobPurpose))
            {
                sb.AppendLine($"JOB PURPOSE");
                sb.AppendLine($"{jobDescription.JobPurpose}");
                sb.AppendLine();
            }

            // Job Duties and Responsibilities
            if (!string.IsNullOrEmpty(jobDescription.JobDutiesAndResponsibilities))
            {
                sb.AppendLine($"DUTIES & RESPONSIBILITIES");
                sb.AppendLine($"{jobDescription.JobDutiesAndResponsibilities}");
                sb.AppendLine();
            }

            // Required Qualifications
            if (!string.IsNullOrEmpty(jobDescription.RequiredQualifications))
            {
                sb.AppendLine($"REQUIRED QUALIFICATIONS");
                sb.AppendLine($"{jobDescription.RequiredQualifications}");
                sb.AppendLine();
            }

            // Preferred Qualifications
            if (!string.IsNullOrEmpty(jobDescription.PreferredQualifications))
            {
                sb.AppendLine($"PREFERRED QUALIFICATIONS");
                sb.AppendLine($"{jobDescription.PreferredQualifications}");
                sb.AppendLine();
            }

            // Working Conditions
            if (string.IsNullOrEmpty(jobDescription.WorkingConditions)) return sb.ToString();

            sb.AppendLine($"WORKING CONDITIONS");
            sb.AppendLine($"{jobDescription.WorkingConditions}");

            return sb.ToString();
        }

    }
}
