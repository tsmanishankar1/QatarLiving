using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s.Classified
{
    public class ClassifiedsBaseDTO
    {
        public AdTypeEnum AdType { get; set; }

        public string Title { get; set; }

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

        [RegularExpression(@"^\+?[1-9]\d{7,14}$", ErrorMessage = "Invalid contact number format.")]
        public string ContactNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string ContactEmail { get; set; }

        [RegularExpression(@"^\+?[1-9]\d{7,14}$", ErrorMessage = "Invalid WhatsApp number format.")]
        public string WhatsAppNumber { get; set; }

        public string? StreetNumber { get; set; }

        public string? BuildingNumber { get; set; }

        public string? zone { get; set; }

        public List<ImageInfo> Images { get; set; }

        public Dictionary<string, string>? Attributes { get; set; }
    }
}
