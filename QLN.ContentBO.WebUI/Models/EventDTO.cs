using System.ComponentModel.DataAnnotations;
using QLN.ContentBO.WebUI.Models;
public class EventDTO
{
    [Required(ErrorMessage = "Category is required.")]
    public EventCategory Category { get; set; }
 
    [Required(ErrorMessage = "Event title is required.")]
    [StringLength(255, ErrorMessage = "Event title cannot exceed 255 characters.")]
    public string EventTitle { get; set; }
 
    [Required(ErrorMessage = "Event Access Type is required.")]
    public EventType EventAccessType { get; set; }
 
    [Range(1, int.MaxValue, ErrorMessage = "Price must be a valid numeric range when Fees is selected.")]
    public string? Price { get; set; }
 
    [Required(ErrorMessage = "Location is required.")]
    public string Location { get; set; }
 
    public string Venue { get; set; }
 
    [Required(ErrorMessage = "Zone is required.")]
    public string ZoneName { get; set; }
 
    public string Longitude { get; set; }
    public string Latitude { get; set; }
 
    [Url(ErrorMessage = "Invalid URL format.")]
    public string? RedirectionLink { get; set; }
 
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
 
    public EventTimeType? EventTimeType { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
 
    public List<PerDayTime> PerDayTimes { get; set; } = new List<PerDayTime>();
 
    [Required(ErrorMessage = "Event description is required.")]
    public string EventDescription { get; set; }
 
    public string? CoverImage { get; set; }
 
    public DateTime PublishedDate { get; set; }
 
    public bool IsActive { get; set; } = true;
}
