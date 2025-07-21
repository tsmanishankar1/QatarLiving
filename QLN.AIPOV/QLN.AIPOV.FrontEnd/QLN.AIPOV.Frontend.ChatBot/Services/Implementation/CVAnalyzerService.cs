using Microsoft.AspNetCore.Components.Forms;
using QLN.AIPOV.Frontend.ChatBot.Models.FormRecognition;
using QLN.AIPOV.Frontend.ChatBot.Services.Interfaces;

namespace QLN.AIPOV.Frontend.ChatBot.Services.Implementation
{
    public class CVAnalyzerService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CVAnalyzerService> logger)
        : ICvAnalyzerService
    {
        public async Task<CVData> AnalyzeCvAsync(IBrowserFile file, CancellationToken cancellationToken = default)
        {
            var endpoint = configuration["FormRecognizer:Endpoint"];
            if (string.IsNullOrEmpty(endpoint))
            {
                throw new InvalidOperationException("API endpoint configuration is missing.");
            }

            var apiUrl = $"{endpoint}/api/Document/analyze-cv";

            using var content = new MultipartFormDataContent();
            await using var fileStream = file.OpenReadStream(maxAllowedSize: 10485760); // 10 MB max
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            var fileContent = new StreamContent(memoryStream);
            content.Add(fileContent, "file", file.Name);

            var response = await httpClient.PostAsync(apiUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<CVData>(cancellationToken) ?? new CVData();
        }
    }
}
