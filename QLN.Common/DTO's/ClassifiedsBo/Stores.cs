using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public class CreateStoresDto
    {
        public string CompanyName { get; set; }
        public string Type { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public List<string> Address { get; set; }
        public string? WhatsAppNumber { get; set; }
        public string? Designation { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Website { get; set; }
        public string? Facebook { get; set; }
        public string? Instagram { get; set; }
        public string? Logo { get; set; }
        public string? Banner { get; set; }
        public string? Description { get; set; }
        public string CRDocument { get; set; }
        public string CRDocumentURL { get; set; }
        public string? ProductDocName { get; set; }
        public string? ProductDocURL { get; set; }
        public int OrderID { get; set; }
        public string Status { get; set; }

    }
    public class Stores
    {
        [Key]
        public Guid StoresID { get; set; }
        
        [Required]
        public string CompanyName { get; set; }
        [Required]
        public string Type { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Phone { get; set; }
        
        [Required]
        public string Designation { get; set; }
        public string WhatsAppNumber { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Website { get; set; }
        public string Facebook { get; set; }
        public string Instagram { get; set; }
        public string Logo { get; set; }
        public string Banner { get; set; }
        public string Description { get; set; }
        public int OrderID { get; set; } = 0;
        public int StoreStatusId { get; set; }

    }

    public class ViewStoresDto
    {
        public Guid StoresID { get; set; }
        public string CompanyName { get; set; }
        public string Type { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public List<string> Address { get; set; }
        public string? WhatsAppNumber { get; set; }
        public string? Designation { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Website { get; set; }
        public string? Facebook { get; set; }
        public string? Instagram { get; set; }
        public string? Logo { get; set; }
        public string? Banner { get; set; }
        public string? Description { get; set; }
        public IList<StoreLicenseDocuments> storeLicenseDocuments { get; set; }
        public IList<StoreProductDocuments> storeProductDocuments { get; set; }
        public int OrderID { get; set; }
        public string Status { get; set; }
    }
    public class StoreAddresses
    {
        [Key]
        public int StoreAddressId { get; set; }
        public string Address { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public bool Status { get; set; }
        
    }
    public class StoreLicenseDocuments
    {
        [Key]
        public int StoreLicenseId { get; set;}
        public string CRDocument { get; set; }
        public string CRDocumentURL { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public bool Status { get; set; } = true;
       
    }
    public class StoreProductDocuments
    {
        [Key]
        public int DocumentId { get; set; }
        public string Document { get; set; }
        public string DocumentURL { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public bool Status { get; set; } = true;
       
    }

    
    public class StoreProducts
    {
        [Key]
        public Guid StoreProductId { get; set; }
        public Guid CompanyId { get; set; }
        public int SubscriptionId { get; set; }
        public string ProductName { get; set; }
        public string ProductLogo { get; set; }
        public decimal ProductPrice { get; set; } = 0;
        public string Currency { get; set; }
        public string ProductSummary { get; set; }
        public string ProductDescription { get; set; }
        public bool Status { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public virtual ICollection<ProductFeatures> Features { get; set; }
        public virtual ICollection<ProductImages> Images { get; set; }
    }
    public class ProductFeatures
    {
        [Key]
        public Guid ProductFeaturesId { get;set;}
        public string Features { get; set; }
        public bool Status { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public Guid StoreProductId { get; set; }
        public virtual StoreProducts StoreProduct { get; set; }
    }
    public class ProductImages
    {
        [Key]
        public Guid ProductImagesId { get; set; }
        public string Images { get; set; }
        public bool Status { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public Guid StoreProductId { get; set; }
        public virtual StoreProducts StoreProduct { get; set; }
    }
    public class StoreProductDto
    {
        public Guid CompanyId { get; set; }
        public int SubscriptionId { get; set; }
        public string ProductName { get; set; }
        public string ProductLogo { get; set; }
        public decimal ProductPrice { get; set; }
        public string Currency { get; set; }
        public string ProductSummary { get; set; }
        public string ProductDescription { get; set; }

        public List<string> Features { get; set; } = new();
        public List<string> Images { get; set; } = new();

        public string CreatedUser { get; set; }
    }
}
