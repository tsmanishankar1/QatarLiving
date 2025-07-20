using System.Text.Json.Serialization;

namespace QLN.AIPOV.Frontend.ChatBot.Models.FormRecognition
{
    public class CVData
    {
        [JsonPropertyName("name")]
        public string? FirstName { get; set; }
        public string? Surname { get; set; }
        public string? Email { get; set; }
        public string? ContactNumber { get; set; }
    }
}
