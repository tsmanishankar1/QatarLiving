using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ServiceAd : AdBase
    {
        public string? ServiceType { get; set; }
        public string? ProviderName { get; set; }
        public string? ProviderLogoUrl { get; set; }
        public string? CertificateFileUrl { get; set; }

        public string? ExpiresOnText { get; set; }

        public string? ServiceCategory { get; set; }
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
        public DateTime? PromotedExpiry { get; set; }
        public DateTime? FeaturedExpiry { get; set; }
        public DateTime? LastRefreshed { get; set; }
        public string? ModerationNotes { get; set; }
    }
}
