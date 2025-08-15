using Azure.Search.Documents.Indexes;
using System;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsDealsIndex
    {
        [SimpleField(IsKey = true)]
        public string Id { get; set; }

        [SearchableField(IsFilterable = true)]
        public string Subvertical { get; set; }

        [SearchableField(IsFilterable = true)]
        public string UserId { get; set; }

        [SearchableField(IsFilterable = true,IsFacetable = true)]
        public string? BusinessName { get; set; }

        [SearchableField(IsFacetable = true, IsFilterable = true)]
        public string? BranchNames { get; set; }

        [SearchableField(IsFilterable = true,IsFacetable = true)]
        public string? Slug { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? BusinessType { get; set; }

        [SearchableField]
        public string? Description { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? StartDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? EndDate { get; set; }

        [SearchableField(IsFilterable = false)]
        public string? FlyerFileUrl { get; set; }

        [SearchableField(IsFilterable = false)]
        public string? DataFeedUrl { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? ContactNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? WhatsappNumber { get; set; }

        [SimpleField]
        public string? WebsiteUrl { get; set; }

        [SimpleField]
        public string? SocialMediaLinks { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool IsActive { get; set; }

        [SimpleField(IsFilterable = true)]
        public string CreatedBy { get; set; }

        [SimpleField(IsSortable = true, IsFilterable = true)]
        public DateTime CreatedAt { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? UpdatedBy { get; set; }

        [SimpleField(IsSortable = true, IsFilterable = true)]
        public DateTime? UpdatedAt { get; set; }

        [SearchableField(IsFilterable = false)]
        public string XMLlink { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? offertitle { get; set; }

        [SimpleField(IsFilterable = true)]
        public string CoverImage { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? ExpiryDate { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool IsFeatured { get; set; } = false;

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? FeaturedExpiryDate { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool IsPromoted { get; set; } = false;

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? PromotedExpiryDate { get; set; }

        [SimpleField(IsFilterable = true)]
        public Guid? SubscriptionId { get; set; }
        public List<string> Locations { get; set; } = new List<string>();
    }
}