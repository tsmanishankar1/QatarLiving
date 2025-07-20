namespace QLN.AIPOV.Backend.Application.Models.Config
{
    public class AzureAISearchSettingsModel
    {
        public string Endpoint { get; set; } = string.Empty;
        public string SearchIndexName { get; set; } = string.Empty;
        public string SearchApiKey { get; set; } = string.Empty;
    }
}
