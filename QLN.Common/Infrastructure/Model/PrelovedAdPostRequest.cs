using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class PrelovedIndex
        {            
        public Guid Id { get; set; }
        public string subVertical { get; set; } = string.Empty;
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Condition { get; set; }
        public double Price { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Colour { get; set; }
        public string Capacity { get; set; }
        public string Processor { get; set; }
        public string Coverage { get; set; }
        public string Ram { get; set; }
        public string Resolution { get; set; }
        public int? BatteryPercentage { get; set; }
        public string SizeType { get; set; }
        public string Size { get; set; }
        public string Gender { get; set; }
        public IFormFile WarrantyCertificate { get; set; }
        public string PhoneNumber { get; set; }
        public string WhatsappNumber { get; set; }
        public string zone { get; set; }
        public string streetNumber { get; set; }
        public string buildingNumber { get; set; }
        public string Storage { get; set; }
        public List<UploadedPhoto> Photos { get; set; } = new List<UploadedPhoto>();
    }

    public class Deals
    {
        public Guid Id { get; set; }
        public string subVertical { get; set; } = string.Empty;
        public string title { get; set; }
        public IFormFile flyerFile { get; set; }
        public string XMLLink { get; set; }
        public DateTime expiryDate { get; set; }
        public string PhoneNumber { get; set; }
        public string WhatsappNumber { get; set; }
        public string location { get; set; }
        public UploadedPhoto Photo { get; set; }
    }

    public class Items
    {
        public Guid Id { get; set; }
        public string subVertical { get; set; } = string.Empty;
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Condition { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string color { get; set; }
        public string capacity { get; set; }
        public string processor { get; set; }
        public string coverage { get; set; }
        public string ram { get; set; }
        public string resolution { get; set; }
        public int? batteryPercentage { get; set; }
        public string sizeType { get; set; }
        public string size { get; set; }
        public string gender { get; set; }
        public IFormFile warrantyCertificate { get; set; }
        public string phoneNumber { get; set; }
        public string whatsappNumber { get; set; }
        public string zone { get; set; }
        public string streetNumber { get; set; }
        public string buildingNumber { get; set; }
        public List<UploadedPhoto> Photos { get; set; } = new List<UploadedPhoto>();

        public class UploadedPhoto
        {
            public Guid Id { get; set; }
            public IFormFile Url { get; set; }
            public bool IsCoverPhoto { get; set; }
            public int? sortOrder { get; set; }
        }
    }
}