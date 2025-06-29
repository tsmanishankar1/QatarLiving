namespace QLN.ContentBO.WebUI.Models
{
    public class EventResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public EventCategory? Category { get; set; }
        public string EventTitle { get; set; }
        public EventType EventAccessType { get; set; }
        public string? Price { get; set; }
        public string Location { get; set; }
        public string Venue { get; set; }
        public string? ZoneName { get; set; }
        public string? Longitude { get; set; }
        public string? Latitude { get; set; }
        public string RedirectionLink { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public EventTimeType? EventTimeType { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public List<PerDayTime> PerDayTimes { get; set; } = new List<PerDayTime>();
        public string EventDescription { get; set; }
        public string? CoverImage { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime PublishedDate { get; set; }
    }
}