using System;
using System.Collections.Generic;

namespace QLN.ContentBO.WebUI.Models
{
    /// <summary>
    /// ClassifiedsItemsApiResponse
    /// </summary>
    public class ClassifiedsApiResponse
    {
        public int TotalCount { get; set; }
        public List<ClassifiedItemViewListing> ClassifiedsItems { get; set; } = new();
    }

    /// <summary>
    /// ClassifiedsCollectiblesApiResponse
    /// </summary>
     public class ClassifiedsCollectiblesApiResponse
    {
        public int TotalCount { get; set; }
        public List<ClassifiedItemViewListing> ClassifiedsCollectibles { get; set; } = new();
    }


    public class ClassifiedItemViewListing
    {
        public string Id { get; set; }
        public string SubVertical { get; set; }
        public string AdType { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string PriceType { get; set; }
        public string CategoryId { get; set; }
        public string Category { get; set; }
        public string L1CategoryId { get; set; }
        public string L1Category { get; set; }
        public string L2CategoryId { get; set; }
        public string L2Category { get; set; }
        public string Location { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Status { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public double Lattitude { get; set; }
        public double Longitude { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public List<ClassifiedImage> Images { get; set; } = new();
        public string AttributesJson { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime? FeaturedExpiryDate { get; set; }
        public bool IsPromoted { get; set; }
        public DateTime? PromotedExpiryDate { get; set; }
        public bool IsRefreshed { get; set; }
        public DateTime? RefreshExpiryDate { get; set; }
    }

    public class ClassifiedImage
    {
        public string AdImageFileNames { get; set; }
        public string Url { get; set; }
        public int Order { get; set; }
    }
}
