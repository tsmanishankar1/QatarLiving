namespace QLN.SearchService.IndexModels
{
    public class AzureSearchSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public Dictionary<string, string> Indexes { get; set; } = new();
    }
}
