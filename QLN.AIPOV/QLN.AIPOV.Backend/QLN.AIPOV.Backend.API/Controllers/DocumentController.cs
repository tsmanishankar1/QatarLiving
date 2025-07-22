using Microsoft.AspNetCore.Mvc;
using QLN.AIPOV.Backend.Application.Interfaces;
using QLN.AIPOV.Backend.Application.Models.FormRecognition;

namespace QLN.AIPOV.Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController(IDocumentService documentService) : ControllerBase
    {
        [HttpPost("analyze-cv")]
        public async Task<ActionResult<CVData>> AnalyzeCV(IFormFile? file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            await using var stream = file.OpenReadStream();
            var contentType = file.ContentType;

            var result = await documentService.ProcessCVAsync(stream, contentType, cancellationToken);

            if (!result.IsSuccessful)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
