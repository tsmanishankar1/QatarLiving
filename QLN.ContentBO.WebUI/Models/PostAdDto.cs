using System.Collections.Generic;

namespace QLN.ContentBO.WebUI.Models
{
    public class PostAdDto
    {
        public int AdType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string PriceType { get; set; }

        public string CategoryId { get; set; }
        public string Category { get; set; }

        public string L1CategoryId { get; set; }
        public string L1Category { get; set; }

        public string L2CategoryId { get; set; }
        public string L2Category { get; set; }

        public string Brand { get; set; }
        public string Model { get; set; }
        public string Condition { get; set; }
        public string Color { get; set; }

        public string Location { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string ContactNumber { get; set; }
        public string ContactEmail { get; set; }
        public string WhatsAppNumber { get; set; }

        public string StreetNumber { get; set; }
        public string BuildingNumber { get; set; }

        public string Zone { get; set; }

        public List<AdImageDto> Images { get; set; } = new();

        public Dictionary<string, string> Attributes { get; set; } = new();
    }

    public class AdImageDto
    {
        public string Url { get; set; }
        public int Order { get; set; }
    }
}
