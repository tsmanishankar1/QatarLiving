using Azure.Search.Documents.Indexes;

namespace QLN.SearchService.IndexModels
{
    public class ClassifiedIndex
    {
        [SimpleField(IsKey = true)]
        public string Id { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string Title { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string Description { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true)]
        public bool IsFeatured { get; set; } = false;

        [SearchableField(IsFilterable = true)]
        public string Category { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string L1Category { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string L2Category { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string StoreName { get; set; }

        [SearchableField(IsFilterable = true)]
        public string StoreLogoUrl { get; set; }

        [SearchableField(IsFilterable = true)]
        public string BannerGroup { get; set; }

        [SearchableField(IsFilterable = true)]
        public string BannerImageUrl { get; set; }

        [SearchableField(IsFilterable = true)]
        public string BannerTargetUrl { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public double Price { get; set; }

        [SearchableField(IsFilterable = true)]
        public string Location { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string Make { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string Model { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string Condition { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string Storage { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string Colour { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string Coverage { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string Size { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string Gender { get; set; } = string.Empty;

        [SimpleField(IsSortable = true)]
        public DateTime CreatedDate { get; set; }
    }
}
