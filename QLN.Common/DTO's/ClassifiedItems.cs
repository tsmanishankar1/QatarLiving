using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedItems
    {
        public string SubVertical { get; set; } = "Items";
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public decimal Price { get; set; }
        public string Condition { get; set; }
        public string Color { get; set; }
        public string Capacity { get; set; }
        public string Processor { get; set; }
        public string Coverage { get; set; }
        public string Ram { get; set; }
        public string Resolution { get; set; }
        public int? BatteryPercentage { get; set; }
        public string? SizeType { get; set; }
        public string? SizeValue { get; set; }
        public string Gender { get; set; }
        public string CertificateBase64 { get; set; }
        public string CertificateFileName { get; set; } 
        public List<string> AdImageFileNames { get; set; } = new(); 
        public List<string> AdImagesBase64 { get; set; } = new();
        public string PhoneNumber { get; set; }
        public string WhatsAppNumber { get; set; }
        public string Zone { get; set; }
        public string StreetNumber { get; set; }
        public string BuildingNumber { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Guid UserId { get; set; }
    }
}
