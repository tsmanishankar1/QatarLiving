using QLN.AIPOV.Backend.Application.Interfaces;
using QLN.AIPOV.Backend.Application.Models.FormRecognition;

namespace QLN.AIPOV.Backend.Domain.Services
{
    public class DocumentService(IDocumentIntelligenceClient documentIntelligenceClient) : IDocumentService
    {
        public async Task<CVData> ProcessCVAsync(Stream documentStream, string contentType, CancellationToken cancellationToken = default)
        {
            return await documentIntelligenceClient.ExtractCVDataAsync(documentStream, contentType, cancellationToken);
        }
    }
}