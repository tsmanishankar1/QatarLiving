using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s.Classified
{
    public class ClassifiedsBaseDTO
    {
        public AdTypeEnum AdType { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public double? Price { get; set; }

        public string? PriceType { get; set; }

        public string? CategoryId { get; set; }

        public string? Category { get; set; }

        public string? L1CategoryId { get; set; }

        public string? L1Category { get; set; }

        public string? L2CategoryId { get; set; }

        public string? L2Category { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Condition { get; set; }
        public string? Color { get; set; }

        public string? Location { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        [RegularExpression(@"^\+?\d{1,4}$", ErrorMessage = "Country code must be between 1 and 4 digits.")]
        public string ContactNumberCountryCode { get; set; } = string.Empty;

        [RegularExpression(@"^\d{7,15}$", ErrorMessage = "Phone number must be between 7 and 15 digits.")]
        public string ContactNumber { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string ContactEmail { get; set; } = string.Empty;

        [RegularExpression(@"^\+?\d{1,4}$", ErrorMessage = "Country code must be between 1 and 4 digits.")]
        public string WhatsappNumberCountryCode { get; set; } = string.Empty;


        [RegularExpression(@"^\d{7,15}$", ErrorMessage = "WhatsApp number must be between 7 and 15 digits.")]
        public string WhatsAppNumber { get; set; } = string.Empty;

        public string? StreetNumber { get; set; }

        public string? BuildingNumber { get; set; }

        public string? zone { get; set; }

        public List<ImageInfo> Images { get; set; } = new List<ImageInfo>();

        public Dictionary<string, string>? Attributes { get; set; }
    }
}
