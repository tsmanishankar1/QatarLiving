namespace QLN.ContentBO.WebUI.Models
{
    public class BannerType
    {
     public Guid Id { get; set; }
     public Vertical VerticalId { get; set; }
     public SubVertical? SubVerticalId { get; set; }
     public string? VerticalName { get; set; }  
     public string? SubVerticalName { get; set; } 
     public List<BannerPageLocationDto>? Pages { get; set; }

    }
}
