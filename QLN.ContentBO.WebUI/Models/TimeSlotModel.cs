using System.ComponentModel.DataAnnotations;
using QLN.ContentBO.WebUI.Models;
using System.ComponentModel.DataAnnotations;

namespace QLN.ContentBO.WebUI.Models
{
    public class TimeSlotModel
    {
        public DayOfWeek? DayOfWeek { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
    }

}