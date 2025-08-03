using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s
{
    public class ServiceDto
    {
        [Required]
        public Guid CategoryId { get; set; }
        [Required]
        public Guid L1CategoryId { get; set; }
        [Required]
        public Guid L2CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? L1CategoryName { get; set; }
        public string? L2CategoryName { get; set; }
        public bool IsPriceOnRequest { get; set; }
        public decimal? Price { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public string PhoneNumberCountryCode { get; set; } = string.Empty;
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;
        [Required]
        public string WhatsappNumberCountryCode { get; set; } = string.Empty;
        [Required]
        public string WhatsappNumber { get; set; } = string.Empty;
        [EmailAddress]
        public string? EmailAddress { get; set; }
        [Required]
        public string Location { get; set; } = string.Empty;
        public int? LocationId { get; set; }
        [Required]
        public string ZoneId { get; set; } = string.Empty;
        public string? StreetNumber { get; set; }
        public string? BuildingNumber { get; set; }
        public string? LicenseCertificate { get; set; }
        public string? Comments { get; set; }
        public decimal Longitude { get; set; }
        public decimal Lattitude { get; set; }
        public List<ImageDto>? PhotoUpload { get; set; }
        public ServiceStatus? Status { get; set; }
        public ServiceAdType AdType { get; set; }
    }
    public class ServiceRequest : ServiceDto
    {
        public string CreatedBy { get; set; }
        public string userName { get; set; }
    }
}
