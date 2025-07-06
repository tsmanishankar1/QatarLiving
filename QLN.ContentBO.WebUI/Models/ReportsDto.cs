namespace QLN.ContentBO.WebUI.Models
{
  public class ReportApiResponseDto
{
    public List<ReportDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

public class ReportDto
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public string Post { get; set; }
    public Guid CommentId { get; set; }
    public string Reporter { get; set; }
    public DateTime ReportDate { get; set; }
    public string? Comment { get; set; }     // Also nullable in your JSON
    public string? UserName { get; set; }    // Also nullable in your JSON
    public DateTime? CommentDate { get; set; }  // ✅ Make nullable

    public int Number { get; set; }
    public string Category => "N/A";
}


}