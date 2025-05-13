using Azure.Search.Documents.Indexes;

namespace QLN.SearchService.IndexModels
{
    public class ClassifiedIndex
    {
        [SimpleField(IsKey = true)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Title { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Description { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true)]
        public bool IsFeaturedItem { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool? IsFeaturedCategory { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool? IsFeaturedStore { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Category { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string L1Category { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string L2Category { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string StoreName { get; set; }

        [SearchableField(IsFilterable = true)]
        public string StoreLogoUrl { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public double Price { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Location { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Make { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Model { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Condition { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Storage { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Colour { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Coverage { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Size { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Gender { get; set; } = string.Empty;

        [SimpleField(IsSortable = true)]
        public DateTime CreatedDate { get; set; }
    }
}
