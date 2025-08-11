using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace QLN.Common.DTO_s.ClassifiedsBo
{
    [XmlRoot("StoreFlyer")]
    public class StoreFlyer
    {
        public string? SubscriptionId {  get; set; }
        public string? CompanyId { get; set; }
        public string? FlyerId { get; set; }
        public List<Product> Products { get; set; } = new();
    }
    public class Product
    {
        public string ProductName { get; set; }
        public string ProductLogo { get; set; }
        public decimal ProductPrice { get; set; } = 0;
        public string Currency { get; set; }
        public string? ProductSummary { get; set; }
        public string? ProductDescription { get; set; }
        public int PageNumber { get; set; } = 1;
       
        public string? PageCoordinates { get; set; }

        [XmlArray("Images")]
        [XmlArrayItem("ProductImage")]
        public List<string> Images { get; set; } = new();

        [XmlArray("Features")]
        [XmlArrayItem("Feature")]
        public List<string> Features { get; set; } = new();
    }

    public class PageCoordinates
    {
        public int? StartPixHorizontal { get; set; } = 0;
        public int? StartPixVertical { get; set; } = 0;
        public int? Height { get; set; } = 0;
        public int? Width { get; set; } = 0;
    }
}
