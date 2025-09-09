using System.ComponentModel.DataAnnotations;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Models
{
    public class CollectiblesEditAdPost : PreviewAdDto
    {
        // ----------------------------
        // Category Selection
        // ----------------------------
        [Required(ErrorMessage = "Category is required.")]
        public long? CategoryId { get; set; }
        public long? L1CategoryId { get; set; }
        public long? L2CategoryId { get; set; }

        // ----------------------------
        // Dynamic Fields
        // ----------------------------
        public Dictionary<string, string> DynamicFields { get; set; } = new();

        // ----------------------------
        // Description and Features
        // ----------------------------
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100, ErrorMessage = "Title must be less than 100 characters.")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string? Description { get; set; }

        [Url(ErrorMessage = "Invalid XML link URL.")]
        public string? XmlLink { get; set; }
        public string? Certificate { get; set; }

        public string? CertificateFileName { get; set; }
        public bool HasAuthenticityCertificate { get; set; }
        public string? AuthenticityCertificateName { get; set; }
        public string? AuthenticityCertificateUrl { get; set; }
        public bool HasWarranty { get; set; }
        public bool IsHandmade { get; set; }
        public string? YearOrEra { get; set; }
        public string? FlyerLocation { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public int Price { get; set; }

        // ----------------------------
        // Contact Details
        // ----------------------------
        [Required(ErrorMessage = "Phone code is required.")]
        [StringLength(5, ErrorMessage = "Phone code must be less than 5 characters.")]
        public string? ContactNumberCountryCode { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        public string? ContactNumber { get; set; }

        [StringLength(5, ErrorMessage = "WhatsApp code must be less than 5 characters.")]
        public string? WhatsappNumberCountryCode { get; set; }

        [Phone(ErrorMessage = "Invalid WhatsApp number.")]
        public string? WhatsappNumber { get; set; }

        // ----------------------------
        // Location
        // ----------------------------
        [StringLength(50, ErrorMessage = "Zone must be less than 50 characters.")]
        public string? Zone { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Street number must be a positive value.")]
        public string? StreetNumber { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Building number must be a positive value.")]
        public string? BuildingNumber { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // ----------------------------
        // Agreement
        // ----------------------------
        [Required(ErrorMessage = "You must agree to the terms.")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms.")]
        public bool IsAgreed { get; set; } = true;

        // ----------------------------
        // Images
        // ----------------------------
        public List<AdImage> Images { get; set; } = new()
        {
            new AdImage { Order = 0 },
            new AdImage { Order = 1 },
            new AdImage { Order = 2 }
        };

        // ----------------------------
        // NEW FIELDS
        // ----------------------------

        // Main fields
        public string? Location { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Condition { get; set; }
        public string? Color { get; set; }

        // Dynamic API attributes
        public Dictionary<string, string>? Attributes { get; set; }
        public long? Id { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? ContactEmail { get; set; }

        public int? Status { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPromoted { get; set; }
        public bool IsRefreshed { get; set; }
        public DateTime? RefreshExpiryDate { get; set; }
        public DateTime? FeaturedExpiryDate { get; set; }
        public DateTime? PromotedExpiryDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? AdType { get; set; } 
        public int? SubVertical { get; set; }

    }
}
