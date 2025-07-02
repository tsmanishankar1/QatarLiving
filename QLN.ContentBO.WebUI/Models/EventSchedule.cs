using System.ComponentModel.DataAnnotations;
using QLN.ContentBO.WebUI.Models;
using System.ComponentModel.DataAnnotations;

namespace QLN.ContentBO.WebUI.Models
{
    public class EventScheduleModel
    {
        [Required]
        public DateOnly StartDate { get; set; }
        [Required]
        public DateOnly EndDate { get; set; }
        public EventTimeType TimeSlotType { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public List<TimeSlotModel>? TimeSlots { get; set; } = [];

    }
}