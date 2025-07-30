using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class FileUploadService : ServiceBase<FileUploadService>, IFileUploadService
    {
        private readonly HttpClient _httpClient;

        public FileUploadService(HttpClient httpClient, ILogger<FileUploadService> Logger) : base(httpClient, Logger)
        {
            _httpClient = httpClient;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> UploadFileAsync(FileUploadModel fileUploadModelData)
        {
            try
            {
                var fileUploadJson = new StringContent(JsonSerializer.Serialize(fileUploadModelData), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, "files/upload")
                {
                    Content = fileUploadJson
                };

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "UploadFileAsync");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
