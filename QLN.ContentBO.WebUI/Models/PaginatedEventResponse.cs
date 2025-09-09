using QLN.ContentBO.WebUI.Models;
using Microsoft.AspNetCore.Mvc;

public class PaginatedEventResponse

{

    public List<EventDTO> Items { get; set; } = new();

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PerPage { get; set; }

    public int FeaturedCount { get; set; }

    public int FeaturedInCurrentPage { get; set; }

}
 public class GetPagedEventsRequest
{
    public int? Page { get; set; }
    public int? PerPage { get; set; }
    public EventStatus? Status { get; set; }
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string? SortOrder { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? FilterType { get; set; }
    public List<int>? LocationId { get; set; }
    public bool? FreeOnly { get; set; }
    public bool? FeaturedFirst { get; set; }
}