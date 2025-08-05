using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace QLN.Common.DTO_s.ClassifiedsBo
{
    [XmlRoot("Products")]
    public class Products
    {
        [XmlElement("Product")]
        public List<Product> ProductList { get; set; } = new();
    }

    public class Product
    {
        public string ProductName { get; set; }
        public string ProductLogo { get; set; }
        public decimal ProductPrice { get; set; }
        public string Currency { get; set; }

        public ProductDetails ProductDetails { get; set; }
    }

    public class ProductDetails
    {
        public string ProductName { get; set; }
        public string ProductSummary { get; set; }
        public string ProductDescription { get; set; }

        [XmlArray("Images")]
        [XmlArrayItem("ProductImage")]
        public List<string> Images { get; set; } = new();

        [XmlArray("Features")]
        [XmlArrayItem("Feature")]
        public List<string> Features { get; set; } = new();
    }
}
