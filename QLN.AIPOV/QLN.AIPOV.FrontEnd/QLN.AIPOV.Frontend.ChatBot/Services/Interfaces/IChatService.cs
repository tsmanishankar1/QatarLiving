using QLN.AIPOV.Frontend.ChatBot.Models.Responses;

namespace QLN.AIPOV.FrontEnd.ChatBot.Services.Interfaces
{
    public interface IChatService
    {
        Task<JobDescriptionsResponse> GetMessagesAsync(string prompt);
    }
}
