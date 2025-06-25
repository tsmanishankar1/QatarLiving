using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class ItemAd : AdBase
    {
        public string? DocType { get; set; }                
        public string? CategoryImageUrl { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public string? Capacity { get; set; }
        public string? Storage { get; set; }
        public string? Colour { get; set; }
        public string? Coverage { get; set; }
        public string? SizeType { get; set; }
        public string? Size { get; set; }
        public string? Gender { get; set; }
        public string? FlyerFileName { get; set; }
        public string? FlyerCoverImageUrl { get; set; }
        public string? FlyerXmlLink { get; set; }
        public string? BatteryPercentage { get; set; }
        public bool? HasWarrantyCertificate { get; set; }
        public string? WarrantyCertificateUrl { get; set; }
        public string? Processor { get; set; }
        public string? Ram { get; set; }
        public string? Resolution { get; set; }
        public string? StoreName { get; set; }
        public string? StoreLogoUrl { get; set; }        
    }
}
