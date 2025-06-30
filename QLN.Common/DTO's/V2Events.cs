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
        [Required]
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class EventSchedule
    {
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public V2EventTimeType TimeSlotType { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public List<TimeSlot> TimeSlots { get; set; } = [];
    }

    public class TimeSlot
    {
        public DayOfWeek? DayOfWeek { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
    }
    public class EventsCategory
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
    }
}
