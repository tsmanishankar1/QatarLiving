using QLN.AIPOV.Frontend.ChatBot.Models.Requests;
using QLN.AIPOV.Frontend.ChatBot.Models.Responses;
using QLN.AIPOV.FrontEnd.ChatBot.Services.Interfaces;

namespace QLN.AIPOV.FrontEnd.ChatBot.Services.Implementation
{
    public class ChatService(HttpClient httpClient) : ServiceBase(httpClient), IChatService
    {
        public async Task<JobDescriptionsResponse> GetMessagesAsync(string prompt)
        {
            var request = new ChatRequest
            {
                Message = prompt
            };
            var response = await PostAsync<ChatRequest, JobDescriptionsResponse>("api/chat", request);

            return response ?? new();
        }
    }
}
