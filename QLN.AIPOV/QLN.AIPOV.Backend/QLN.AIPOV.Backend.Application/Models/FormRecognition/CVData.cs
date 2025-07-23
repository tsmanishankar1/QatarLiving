namespace QLN.AIPOV.Backend.Application.Models.FormRecognition
{
    public class CVData
    {
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string ResumeText { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
