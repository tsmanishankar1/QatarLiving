using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s
{
    public class ServicesDto
    {
        public Guid Id { get; set; }
        public Guid MainCategoryId { get; set; }
        public Guid L1CategoryId { get; set; }
        public Guid L2CategoryId { get; set; }
        public string? Price { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string PhoneNumberCountryCode { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string WhatsappNumberCountryCode { get; set; }
        [Required]
        public string WhatsappNumber { get; set; }
        [EmailAddress]
        public string? EmailAddress { get; set; }
        public string Location { get; set; }
        public int? LocationId { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public List<ImageDto>? PhotoUpload { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class ImageDto
    {
        public string? FileName { get; set; }
        public string? Url { get; set; }
        public int Order { get; set; }
    }
    public class DeleteServiceRequest
    {
        public Guid Id { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
