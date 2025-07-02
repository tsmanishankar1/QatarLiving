using QLN.Web.Shared.Models;
using QLN.Common.DTO_s;
public class SearchStateService
{
    public string ItemSearchText { get; set; }
    public string ItemCategory { get; set; }
    public string ItemSubCategory { get; set; } 
    public string ItemSubSubCategory { get; set; }  
    public string SelectedCategoryName { get; set; }
    public string SelectedSubCategoryName { get; set; }
    public string SelectedSubSubCategoryName { get; set; }

    public string ItemBrand { get; set; }
    public long? ItemMinPrice { get; set; }
    public long? ItemMaxPrice { get; set; }
    public string ItemViewMode { get; set; } = "grid";
    public bool ItemHasWarrantyCertificate { get; set; } = false;

    public Dictionary<string, List<string>> ItemFieldFilters { get; set; } = new();

    public List<CategoryTreeDto> ItemCategoryTrees
    {
        get => _itemCategoryTrees;
        set
        {
            _itemCategoryTrees = value;
            OnCategoryTreesChanged?.Invoke();
        }
    }
    private List<CategoryTreeDto> _itemCategoryTrees = new();
    public List<CategoryField> ItemCategoryFilters = new();
    public event Action? OnCategoryTreesChanged;

    public string? ItemSortBy { get; set; }
    public bool IsSearchActive =>
    !string.IsNullOrWhiteSpace(ItemSearchText) ||
    !string.IsNullOrWhiteSpace(ItemCategory) ||
    !string.IsNullOrWhiteSpace(ItemBrand) ||
    ItemMinPrice.HasValue ||
    ItemMaxPrice.HasValue;



    public string CollectiblesSearchText { get; set; }
    public string CollectiblesCategory { get; set; }
    public string CollectiblesSubCategory { get; set; } 
    public string CollectiblesSubSubCategory { get; set; }  
    public string CollectiblesCondition { get; set; }
    public long? CollectiblesMinPrice { get; set; }
    public long? CollectiblesMaxPrice { get; set; }
    public string CollectiblesViewMode { get; set; } = "grid";
    public Dictionary<string, List<string>> CollectiblesFilters { get; set; } = new();
    public bool CollectiblesHasAuthenticityCertificate { get; set; } = false;
    public List<CategoryTreeDto> CollectiblesCategoryTrees { get; set; } = new();

    public string? CollectiblesSortBy { get; set; }
    public bool IsCollectiblesSearchActive =>
    !string.IsNullOrWhiteSpace(CollectiblesSearchText) ||
    !string.IsNullOrWhiteSpace(CollectiblesCategory) ||
    !string.IsNullOrWhiteSpace(CollectiblesCondition) ||
    CollectiblesMinPrice.HasValue ||
    CollectiblesMaxPrice.HasValue;


    public string PrelovedSearchText { get; set; }
    public string PrelovedCategory { get; set; }
    public string PrelovedSubCategory { get; set; } 
    public string PrelovedSubSubCategory { get; set; }  
    public string PrelovedSelectedCategoryName { get; set; }
    public string PrelovedSelectedSubCategoryName { get; set; }
    public string PrelovedSelectedSubSubCategoryName { get; set; }

    public string PrelovedBrand { get; set; }
    public long? PrelovedMinPrice { get; set; }
    public long? PrelovedMaxPrice { get; set; }
    public string PrelovedViewMode { get; set; } = "grid";
    public Dictionary<string, List<string>> PrelovedFilters { get; set; } = new();
    public bool PrelovedHasWarrantyCertificate { get; set; } = false;
    public List<CategoryTreeDto> PrelovedCategoryTrees { get; set; } = new();

    public string? PrelovedSortBy { get; set; }
    public bool IsPrelovedSearchActive =>
    !string.IsNullOrWhiteSpace(PrelovedSearchText) ||
    !string.IsNullOrWhiteSpace(PrelovedCategory) ||
    !string.IsNullOrWhiteSpace(PrelovedBrand) ||
    PrelovedMinPrice.HasValue ||
    PrelovedMaxPrice.HasValue;

}

