namespace QLN.ContentBO.WebUI.Models
{
    public class FileUploadResponse
    {
        public bool IsSuccess { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string Message { get; set; }
    }
}
