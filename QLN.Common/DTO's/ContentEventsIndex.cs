using Azure.Search.Documents.Indexes;
using System;
using System.Collections.Generic;

namespace QLN.Common.DTO_s
{
    public class ContentEventsIndex
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; }

        [SearchableField(IsFilterable = true)]
        public string EventTitle { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public int CategoryId { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string? CategoryName { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public string EventType { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public int? Price { get; set; }

        [SimpleField(IsFilterable = true)]
        public string Location { get; set; }

        [SimpleField(IsFilterable = true)]
        public int? LocationId { get; set; }

        [SimpleField(IsFilterable = true)]
        public string Venue { get; set; }

        [SimpleField(IsFilterable = true)]
        public string Longitude { get; set; }

        [SimpleField(IsFilterable = true)]
        public string Latitude { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? RedirectionLink { get; set; }

        public EventScheduleIndex EventSchedule { get; set; }

        [SearchableField]
        public string EventDescription { get; set; }

        [SimpleField(IsFilterable = true)]
        public string CoverImage { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool IsFeatured { get; set; }

        public SlotIndex? FeaturedSlot { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? PublishedDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public string Status { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public string Slug { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool IsActive { get; set; }

        [SimpleField(IsFilterable = true)]
        public string CreatedBy { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime CreatedAt { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? UpdatedBy { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? UpdatedAt { get; set; }
    }

    public class SlotIndex
    {
        [SimpleField(IsFilterable = true)]
        public int Id { get; set; }

        [SimpleField(IsFilterable = true)]
        public string Name { get; set; }
    }

    public class EventScheduleIndex
    {
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTimeOffset StartDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTimeOffset EndDate { get; set; }

        [SimpleField(IsFilterable = true)]
        public string TimeSlotType { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? GeneralTextTime { get; set; }

        public IList<TimeSlotIndex>? TimeSlots { get; set; } = [];
    }

    public class TimeSlotIndex
    {
        [SimpleField(IsFilterable = true)]
        public string? DayOfWeek { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? TextTime { get; set; }
    }
}
