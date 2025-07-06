using System;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class EventDTOV2
    {
        public Guid Id { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public EventType EventType { get; set; }
        public int? Price { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Venue { get; set; } = string.Empty;
        public string Longitude { get; set; } = string.Empty;
        public string Latitude { get; set; } = string.Empty;
        public string? RedirectionLink { get; set; }
        public EventScheduleModel EventSchedule { get; set; } = new();
        public string EventDescription { get; set; } = string.Empty;
        public string CoverImage { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }
        public Slot FeaturedSlot { get; set; } = new();
        public DateTime? PublishedDate { get; set; }
        public EventStatus Status { get; set; }
        public string Slug { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public enum EventStatus
    {
        Published = 1,
        UnPublished = 2,
        Expired = 3
    }

    public enum EventType
    {
        FreeAcess = 1,
        OpenRegistrations = 2,
        FeePrice = 3
    }

    public class EventScheduleModel
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public EventTimeType TimeSlotType { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public List<TimeSlotModel> TimeSlots { get; set; } = [];
    }

    public enum EventTimeType
    {
        GeneralTime = 1,
        PerDayTime = 2
    }

    public class TimeSlotModel
    {
        public DayOfWeek? DayOfWeek { get; set; }
        public string? Time { get; set; }
    }

    public class Slot
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
