namespace QLN.ContentBO.WebUI.Models
{
  public class SeasonalPicksDto
  {
    public Guid? Id { get; set; }
    public string Title { get; set; } = null!;
    public Vertical Vertical { get; set; }
    public long? CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public long? L1CategoryId { get; set; }
    public string L1categoryName { get; set; } = null!;
    public long? L2categoryId { get; set; }
    public string L2categoryName { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string ImageUrl { get; set; } = null!;
      public int? SlotOrder { get; set; }
    }
}