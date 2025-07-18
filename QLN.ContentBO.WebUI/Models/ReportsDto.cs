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
    public Guid? PostId { get; set; }
    public string Post { get; set; }
    public Guid CommentId { get; set; }
    public string Reporter { get; set; }
    public DateTime ReportDate { get; set; }
    public string? Comment { get; set; }
    public string? UserName { get; set; } 
    public string Slug { get; set; }
    public DateTime? CommentDate { get; set; } 
    public DateTime? PostDate { get; set; } 

    public int Number { get; set; }
    public string Category => "N/A";
}


}