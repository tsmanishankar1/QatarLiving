using System.ComponentModel.DataAnnotations;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Models
{
    public class AdPost
    {
        // ----------------------------
        // Category Selection
        // ----------------------------

        [Required(ErrorMessage = "Category is required.")]
        public string? SelectedCategoryId { get; set; }
        public string? SelectedSubcategoryId { get; set; }
        public string? SelectedSubSubcategoryId { get; set; }

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

        [StringLength(50, ErrorMessage = "Certificate name must be less than 50 characters.")]
        public string? Certificate { get; set; }

        public string? CertificateFileName { get; set; }

        [Range(0, 100, ErrorMessage = "Battery percentage must be between 0 and 100.")]
        public int BatteryPercentage { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string? Description { get; set; }

        [Url(ErrorMessage = "Invalid XML link URL.")]
        public string? XmlLink { get; set; }


        [Range(1, int.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public int Price { get; set; }

        // ----------------------------
        // Contact Details
        // ----------------------------

        [Required(ErrorMessage = "Phone code is required.")]
        [StringLength(5, ErrorMessage = "Phone code must be less than 5 characters.")]
        public string? PhoneCode { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\d{7,15}$", ErrorMessage = "Invalid phone number (7-15 digits only).")]
        public string? PhoneNumber { get; set; }

        [StringLength(5, ErrorMessage = "WhatsApp code must be less than 5 characters.")]
        public string? WhatsappCode { get; set; }

        [Phone(ErrorMessage = "Invalid WhatsApp number.")]
        public string? WhatsappNumber { get; set; }

        // ----------------------------
        // Location
        // ----------------------------

        [StringLength(50, ErrorMessage = "Zone must be less than 50 characters.")]
        public string? Zone { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Street number must be a positive value.")]
        public int? StreetNumber { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Building number must be a positive value.")]
        public int? BuildingNumber { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // ----------------------------
        // Agreement
        // ----------------------------

        [Required(ErrorMessage = "You must agree to the terms.")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms.")]
        public bool IsAgreed { get; set; }

        // ----------------------------
        // Images
        // ----------------------------
        public List<AdImage> Images { get; set; } = new()
        {
            new AdImage { Order = 0 },
            new AdImage { Order = 1 },
            new AdImage { Order = 2 }
        };

    }
       

}
