namespace QLN.AIPOV.Backend.Application.Models.Config
{
    public class OpenAISettingsModel
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
    }
}
