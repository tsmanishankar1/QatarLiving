using QLN.ContentBO.WebUI.Helpers;

namespace QLN.ContentBO.WebUI.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Converts the given UTC DateTime to Qatar time and returns a formatted string.
        /// </summary>
        public static string ToQatarTime(this DateTime utcDateTime, string format = "yyyy-MM-dd HH:mm")
        {
            return DateTimeDisplayHelper.ToQatarTimeString(utcDateTime, format);
        }

        /// <summary>
        /// Converts the given UTC DateTime to Qatar time and returns a DateTime.
        /// </summary>
        public static DateTime ToQatarTime(this DateTime utcDateTime)
        {
            return DateTimeDisplayHelper.ToQatarTime(utcDateTime);
        }

    }
}
