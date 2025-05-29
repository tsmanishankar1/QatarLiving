using System.Globalization;

namespace QLN.Web.Shared.Helpers
{
    public class DateHelper
    {
        public static string FormatDate(string dateString)
        {
            try
            {
                var parsed = DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                return parsed.ToString("dd MMM");
            }
            catch
            {
                return dateString;
            }
        }
    }
}
