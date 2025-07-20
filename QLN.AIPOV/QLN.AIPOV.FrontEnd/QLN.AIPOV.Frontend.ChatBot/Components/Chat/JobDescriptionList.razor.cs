using Microsoft.AspNetCore.Components;
using QLN.AIPOV.Frontend.ChatBot.Models.Chat;

namespace QLN.AIPOV.Frontend.ChatBot.Components.Chat
{
    public partial class JobDescriptionList
    {
        [Parameter]
        public EventCallback<JobDescription> SelectedValueChanged { get; set; }

        [Parameter]
        public List<JobDescription>? JobDescriptions { get; set; }

        public JobDescription? SelectedValue { get; set; }

        private async Task OnSelectedValueChanged(JobDescription? message)
        {
            if (message == null) return;
            SelectedValue = message;
            await SelectedValueChanged.InvokeAsync(message);
        }

    }
}
