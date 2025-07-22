using Microsoft.AspNetCore.Components.Forms;
using QLN.AIPOV.Frontend.ChatBot.Models.FormRecognition;

namespace QLN.AIPOV.Frontend.ChatBot.Services.Interfaces
{
    public interface ICvAnalyzerService
    {
        Task<CVData> AnalyzeCvAsync(IBrowserFile file, CancellationToken cancellationToken = default);
    }
}
