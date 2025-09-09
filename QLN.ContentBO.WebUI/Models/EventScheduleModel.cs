using System.ComponentModel.DataAnnotations;

namespace QLN.ContentBO.WebUI.Models
{
    public class EventScheduleModel
    {
        [Required(ErrorMessage = "Start date is required")]
        public DateOnly StartDate { get; set; }
        
        [Required(ErrorMessage = "End date is required")]
        public DateOnly EndDate { get; set; }
        
        public EventTimeType TimeSlotType { get; set; }
        
        public string? GeneralTextTime { get; set; }
        
        public List<TimeSlotModel>? TimeSlots { get; set; } = [];
    }
}