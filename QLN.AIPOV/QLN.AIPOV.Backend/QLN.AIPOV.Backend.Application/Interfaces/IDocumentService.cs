using QLN.AIPOV.Backend.Application.Models.FormRecognition;

namespace QLN.AIPOV.Backend.Application.Interfaces
{
    public interface IDocumentService
    {
        Task<CVData> ProcessCVAsync(Stream documentStream, string contentType, CancellationToken cancellationToken = default);
    }
}
