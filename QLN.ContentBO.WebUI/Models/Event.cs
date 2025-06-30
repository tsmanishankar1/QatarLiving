using System.ComponentModel.DataAnnotations;
namespace QLN.ContentBO.WebUI.Models
{
    public class Event
    {
        [Required]
        public string EventTitle { get; set; }

        [Required]
        public int CategoryId { get; set; }

        // Yet to Decide Event Types Free, Open Regisration, Fees
        public int Price { get; set; }

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

        public Guid CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public Guid UpdatedBy { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class EventSchedule
    {
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string TimeSlotType { get; set; } // Yet to Decide
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public List<TimeSlot> TimeSlots { get; set; } = [];
    }

    public class TimeSlot
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
    }

    public class EventsCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
