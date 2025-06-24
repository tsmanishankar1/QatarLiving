using QLN.Web.Shared.Models;
using QLN.Common.DTO_s;
public class SearchStateService
{
    public string SearchText { get; set; }
    public string Category { get; set; }
    public string Brand { get; set; }
    public long? MinPrice { get; set; }
    public long? MaxPrice { get; set; }

    public string ViewMode { get; set; } = "grid";
    public List<PromotedItem> Results { get; set; } = new();

    public string ItemSearchText { get; set; }
    public string ItemCategory { get; set; }
    public string ItemBrand { get; set; }
    public long? ItemMinPrice { get; set; }
    public long? ItemMaxPrice { get; set; }
    public string ItemViewMode { get; set; } = "grid";
    public List<CategoryTreeDto> ItemCategoryTrees { get; set; } = new();

    public string? ItemSortBy { get; set; }
    public bool IsSearchActive =>
    !string.IsNullOrWhiteSpace(ItemSearchText) ||
    !string.IsNullOrWhiteSpace(ItemCategory) ||
    !string.IsNullOrWhiteSpace(ItemBrand) ||
    ItemMinPrice.HasValue ||
    ItemMaxPrice.HasValue;

}
