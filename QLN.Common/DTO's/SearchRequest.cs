namespace QLN.Common.DTO_s
{
    public class SearchRequest
    {
        public string? Text { get; set; }
        public int Top { get; set; } = 50;
        public Dictionary<string, object> Filters { get; set; } = new();
        public string? OrderBy { get; set; }
    }
}
