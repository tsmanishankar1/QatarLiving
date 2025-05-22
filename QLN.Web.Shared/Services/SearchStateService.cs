using QLN.Web.Shared.Models;

public class SearchStateService
{
    public string SearchText { get; set; }
    public string Category { get; set; }
    public string Brand { get; set; }
   public long? MinPrice { get; set; }
public long? MaxPrice { get; set; }

    public string ViewMode { get; set; } = "grid";
    public List<PromotedItem> Results { get; set; } = new();
}
