using System.ComponentModel.DataAnnotations;

namespace QLN.AIPOV.Frontend.ChatBot.Models.Requests
{
    public class ChatRequest
    {
        [Required]
        public string Message { get; set; } = string.Empty;
    }
}
