
namespace QLN.Common.DTO_s
{
    public class NotificationDto
    {
        public List<string> Destinations { get; set; } = new();
        public SenderDto? Sender { get; set; }
        public List<RecipientDto> Recipients { get; set; } = new();
        public string Subject { get; set; } = string.Empty;
        public string Plaintext { get; set; } = string.Empty;
        public string? Html { get; set; }
        public List<AttachmentDto>? Attachments { get; set; }
    }

    public class SenderDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class RecipientDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class AttachmentDto
    {
        public string Filename { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
    }
}