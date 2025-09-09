using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.ClassifiedsBo
{
    public class SubscriptionTypes
    {
        [Key]
        public int SubscriptionId { get; set; }
        public string SubscriptionType { get; set; }
    }
    public class StoreStatus
    {
        [Key]
        public int StoreStatusId { get; set; }
        public string Status { get; set; }
    }
   
    public class StoreFlyers
    {
        [Key]
        public Guid StoreFlyersId { get; set; }
        public Guid? SubscriptionId { get; set; } = Guid.Empty;
        public Guid? CompanyId { get; set; }=Guid.Empty;
        public Guid? FlyerId { get; set; }
        public string? FileName { get; set; }
        public virtual ICollection<StoreProducts> Products { get; set; }
    }
    
    public class StoreProducts
    {
        [Key]
        public Guid StoreProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductLogo { get; set; }
        public decimal ProductPrice { get; set; } = 0;
        public string Currency { get; set; } = "QAR";
        public string ProductSummary { get; set; }
        public string ProductDescription { get; set; }
        public int? PageNumber { get; set; } = 1;
        public string?  PageCoordinates { get; set; }
        public string? Slug { get; set; }
        public string? Category { get; set; }
        public int? Qty { get; set; } = 0;
        public string? ProductBarcode { get; set; }
        public Guid FlyerId { get; set; }      
        public virtual StoreFlyers StoreFlyer { get; set; }       
        public virtual ICollection<ProductFeatures> Features { get; set; }
        public virtual ICollection<ProductImages> Images { get; set; }
    }

    public class ProductPageCoordinates
    {
        [Key]
        public Guid PageCoordinatesId { get; set; }
        public int? StartPixHorizontal { get; set; } = 0;
        public int? StartPixVertical { get; set; } = 0;
        public int? Height { get; set; } = 0;
        public int? Width { get; set; } = 0;
      
        //public Guid StoreProductId { get; set; }
        //public virtual StoreProducts StoreProduct { get; set; }
    }
    public class ProductFeatures
    {
        [Key]
        public Guid ProductFeaturesId { get;set;}
        public string Features { get; set; }
      
        public Guid StoreProductId { get; set; }
        public virtual StoreProducts StoreProduct { get; set; }
    }
    public class ProductImages
    {
        [Key]
        public Guid ProductImagesId { get; set; }
        public string Images { get; set; }
        
        public Guid StoreProductId { get; set; }
        public virtual StoreProducts StoreProduct { get; set; }
    }
    
}
