using QLN.Common.DTO_s;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLN.Common.Infrastructure.Model
{
    public class NotificationEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Column(TypeName = "jsonb")]
        public List<string> Destinations { get; set; } = new();

        [Column(TypeName = "jsonb")]
        public SenderDto? Sender { get; set; }

        [Column(TypeName = "jsonb")]
        public List<RecipientDto> Recipients { get; set; } = new();

        [Required]
        [MaxLength(255)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(5000)] 
        public string Plaintext { get; set; } = string.Empty;

        [MaxLength(20000)] 
        public string? Html { get; set; }

        [Column(TypeName = "jsonb")]
        public List<AttachmentDto>? Attachments { get; set; }
    }
}
