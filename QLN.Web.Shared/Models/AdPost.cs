using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QLN.Web.Shared.Models
{
    public class AdPost
    {
        // Category Selection
        [Required]
        public string SelectedVertical { get; set; } = string.Empty;

        public string? SelectedCategoryId { get; set; }
        public string? SelectedSubcategoryId { get; set; }
        public string? SelectedSubSubcategoryId { get; set; }

        // Dynamic Fields (e.g. Brand, Model, etc.)
        public Dictionary<string, string> DynamicFields { get; set; } = new();

        // Description and Features
        public string? Title { get; set; }

        public string? Certificate { get; set; }
        public string? CertificateFileName { get; set; }
        public int BatteryPercentage { get; set; }

        public string? ItemDescription { get; set; }
        public string? XmlLink { get; set; }
        public string? FlyerLocation { get; set; }

        public int Price { get; set; }

        // Contact Details
        public string? PhoneCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WhatsappCode { get; set; }
        public string? WhatsappNumber { get; set; }

        // Location
        public string? Zone { get; set; }
       public int? StreetNumber { get; set; }
public int? BuildingNumber { get; set; }


        // Agreement
        public bool IsAgreed { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Images (assuming image paths or IDs)
        public List<string> PhotoUrls { get; set; } = new() { "", "", "", "", "", "" };
    }
}
