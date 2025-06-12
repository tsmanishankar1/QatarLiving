using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class CommonAdBase
    {
        public string SubVertical { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public string? Section { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public decimal? Price { get; set; }
        public string? Condition { get; set; }
        public string? Color { get; set; }
        public string? Capacity { get; set; }
        public string? Processor { get; set; }
        public string? Coverage { get; set; }
        public string? Ram { get; set; }
        public string? Resolution { get; set; }
        public int? BatteryPercentage { get; set; }
        public string? Gender { get; set; }
        public string? Size { get; set; }
        public string? SizeValue { get; set; }        
        public string? CertificateBase64 { get; set; }
        public string? CertificateFileName { get; set; }
        public List<string> AdImageFileNames { get; set; } = new();
        public List<string> AdImagesBase64 { get; set; } = new();
        public string PhoneNumber { get; set; }
        public string WhatsAppNumber { get; set; }
        public string? ContactEmail { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? CountryOfOrigin { get; set; }
        public int? Language { get; set; }
        public string? Zone { get; set; }
        public string? StreetNumber { get; set; }
        public string? BuildingNumber { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }           
        public Guid UserId { get; set; }
        [DefaultValue(false)]
        public bool? IsFeatured { get; set; } = false;
        [DefaultValue(false)]
        public bool? IsPromoted { get; set; } = false;
        public AdStatus Status { get; set; }

    }
}
