using QLN.ContentBO.WebUI.Models;
public class PaginatedEventResponse

{

    public List<EventDTO> Items { get; set; } = new();

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PerPage { get; set; }

    public int FeaturedCount { get; set; }

    public int FeaturedInCurrentPage { get; set; }

}
 