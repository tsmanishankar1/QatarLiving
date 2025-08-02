using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IFileUploadService
    {
        /// <summary>
        /// Uploads a file to the specified vertical Blob storage.
        /// </summary>
        /// <param name="fileUploadData">Module Name with base64 encoded string</param>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage> UploadFileAsync(FileUploadModel fileUploadData);
    }
}
