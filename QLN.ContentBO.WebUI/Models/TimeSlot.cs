using System.ComponentModel.DataAnnotations;
using QLN.ContentBO.WebUI.Models;
using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s
{
    public class TimeSlot
    {
        public DayOfWeek? DayOfWeek { get; set; }
        public string? Time { get; set; }
    }

}