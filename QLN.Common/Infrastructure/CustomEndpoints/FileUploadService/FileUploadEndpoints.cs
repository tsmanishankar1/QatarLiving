using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace QLN.Common.Infrastructure.CustomEndpoints.FileUploadService
{
    public static class FileUploadEndpoints
    {
        // log out endpoint
        public static RouteGroupBuilder MapFileUploadEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/upload", async Task<Results<Ok<FileUploadResponse>,
            NotFound<ProblemDetails>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>> (
            HttpContext context,
            [FromBody] FileUploadRequest request,
            [FromServices] IFileStorageBlobService fileStorageBlob,
            [FromServices] IEventlogger log,
            CancellationToken cancellationToken
         ) =>
            {
                var allowedContainers = new List<string>
                {
                    "uploads",
                    "somethingelse"
                };

                if(!allowedContainers.Contains(request.Container))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Container Name",
                        Detail = "Failed to upload the file.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var (imgExt, base64Image) = Base64ImageHelper.ParseBase64Image(request.Base64);
                string tenDigitGuid1 = Guid.NewGuid().ToString("N").Substring(0, 10);
                var customName = $"{tenDigitGuid1}.{imgExt}";

                try
                {
                    var fileUpload = await fileStorageBlob.SaveBase64File(base64Image, customName, request.Container, cancellationToken);
                    if (string.IsNullOrEmpty(fileUpload))
                    {
                        log.LogTrace($"Failed to upload file '{customName}' to container '{request.Container}'.");

                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "File Upload Error",
                            Detail = "Failed to upload the file.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    var response = new FileUploadResponse
                    {
                        IsSuccess = true,
                        FileName = customName,
                        FileUrl = fileUpload,
                        Message = "File uploaded successfully."
                    };

                    log.LogTrace($"File '{customName}' uploaded successfully to container '{request.Container}'.");

                    return TypedResults.Ok(response);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        title: "File Upload Error",
                        detail: "An unexpected error occurred during file upload.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("FileUpload")
            .WithTags("FileUpload")
            .WithSummary("Uploads a file to the specified container.")
            .WithDescription("Uploads a file to the specified container in the blob storage. The file is provided as a base64 encoded string.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            return group;
        }
    }
}
