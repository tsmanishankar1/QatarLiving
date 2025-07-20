using System.Globalization;

namespace QLN.ContentBO.WebUI.Helpers
{
    public static class DateTimeDisplayHelper
    {
        private static readonly TimeZoneInfo QatarTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");

        public static DateTime ToQatarTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Unspecified)
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime.ToUniversalTime(), QatarTimeZone);
        }

        public static string ToQatarTimeString(DateTime utcDateTime, string format = "yyyy-MM-dd HH:mm")
        {
            var qatarTime = ToQatarTime(utcDateTime);
            return qatarTime.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
