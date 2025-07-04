using System.ComponentModel.DataAnnotations;
 
namespace QLN.ContentBO.WebUI.Models
{
    public class EventDTO
  {
      public Guid Id { get; set; }
      [Required]
      public string EventTitle { get; set; }
      [Required]
      public int CategoryId { get; set; }
      public EventType EventType { get; set; }
      public int? Price { get; set; }
      [Required]
      public string Location { get; set; }
      public string Venue { get; set; }
      public string Longitude { get; set; }
      public string Latitude { get; set; }
 
      [Url(ErrorMessage = "Invalid URL format.")]
      public string? RedirectionLink { get; set; }
      public EventScheduleModel EventSchedule { get; set; }
 
      [Required(ErrorMessage = "Event description is required.")]
      public string EventDescription { get; set; }
      public string? CoverImage { get; set; }
      public bool IsFeatured { get; set; } = false;
      public Slot FeaturedSlot { get; set; } = new();
      public DateTime? PublishedDate { get; set; }
      public EventStatus Status { get; set; }
      public string Slug { get; set; }
      public bool IsActive { get; set; } = true;
      public string CreatedBy { get; set; }
      public DateTime CreatedAt { get; set; }
      public string? UpdatedBy { get; set; }
      public DateTime? UpdatedAt { get; set; }
  }
}
 public enum EventStatus
{
    Published = 1,
    UnPublished = 2,
    Expired = 3
}