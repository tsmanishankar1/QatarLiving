using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s
{
    public class V2Events
    {
        public Guid Id { get; set; }
        [Required]
        public string EventTitle { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public V2EventType EventType { get; set; }
        public int? Price { get; set; }
        [Required]
        public string Location { get; set; }
        public string Venue { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }

        [Url(ErrorMessage = "Invalid URL format.")]
        public string? RedirectionLink { get; set; }
        public EventSchedule EventSchedule { get; set; }

        [Required(ErrorMessage = "Event description is required.")]
        public string EventDescription { get; set; }
        public string CoverImage { get; set; }
        public bool IsFeatured { get; set; } = false;
        public V2Slot FeaturedSlot { get; set; } = new();
        public DateTime? PublishedDate { get; set; }
        public EventStatus Status { get; set; }
        public string Slug { get; set; }
        public bool IsActive { get; set; } = true;
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class EventSchedule
    {
        [Required]
        public DateOnly StartDate { get; set; }
        [Required]
        public DateOnly EndDate { get; set; }
        public V2EventTimeType TimeSlotType { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public List<TimeSlot>? TimeSlots { get; set; } = [];
    }
    public class TimeSlot
    {
        public DayOfWeek? DayOfWeek { get; set; }
        public string? Time { get; set; }
    }
    public class EventsCategory
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
    }
    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int? Page { get; set; }
        public int? PerPage { get; set; }
        public int FeaturedCount { get; set; }
        public int FeaturedInCurrentPage { get; set; }
    }
    public class EventReorder
    {
        public int FromSlot { get; set; }
        public int ToSlot { get; set; }
        public string? UserId { get; set; }
    }
    public class UpdateFeaturedEvent
    {
        public Guid EventId { get; set; }
        public V2Slot Slot { get; set; }
        public bool IsFeatured { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

}
