namespace QLN.ContentBO.WebUI.Models
{
    public class PerDayTime
    {
        public DayOfWeek Day { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}