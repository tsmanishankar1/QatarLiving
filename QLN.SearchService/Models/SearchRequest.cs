namespace QLN.SearchService.IndexModels
{
    public class SearchRequest
    {
        public string? Text { get; set; }

        public string? Category { get; set; }
        public string? L1Category { get; set; }
        public string? L2Category { get; set; }

        public string? StoreName { get; set; }
        public bool? IsFeaturedItem { get; set; }
        public bool? IsFeaturedCategory { get; set; }
        public bool? IsFeaturedStore { get; set; }

        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }

        public string? Location { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }

        public string? Condition { get; set; }
        public string? Storage { get; set; }
        public string? Colour { get; set; }
        public string? Coverage { get; set; }

        public string? Size { get; set; }
        public string? Gender { get; set; }

        public int Top { get; set; } = 50;
    }
}
