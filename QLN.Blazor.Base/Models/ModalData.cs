namespace QLN.Blazor.Base.Models
{
    public class ModalData
    {
        public bool Open { get; set; } = true;
        public string Type { get; set; } = "error";
        public string Header { get; set; } = "Error";
        public string Body { get; set; } = string.Empty;
        public bool ShowLogout { get; set; } = false;
    }
}