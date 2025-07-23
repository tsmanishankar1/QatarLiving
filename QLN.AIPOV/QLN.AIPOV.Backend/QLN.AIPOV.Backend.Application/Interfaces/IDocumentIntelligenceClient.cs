using QLN.AIPOV.Backend.Application.Models.FormRecognition;

namespace QLN.AIPOV.Backend.Application.Interfaces
{
    public interface IDocumentIntelligenceClient
    {
        Task<CVData> ExtractCVDataAsync(Stream documentStream, string contentType, CancellationToken cancellationToken = default);
    }
}
