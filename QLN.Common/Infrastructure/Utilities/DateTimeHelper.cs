using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Utilities
{
    public static class DateTimeHelper
    {
        public static DateTimeOffset? ToDateTimeOffset(this DateTime? dateTime)
        {
            if (!dateTime.HasValue)
                return null;

            var dt = dateTime.Value;
            return dt.Kind == DateTimeKind.Unspecified
                ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc))
                : new DateTimeOffset(dt);
        }

        public static DateTimeOffset ToDateTimeOffset(this DateTime dateTime)
        {
            return dateTime.Kind == DateTimeKind.Unspecified
                ? new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc))
                : new DateTimeOffset(dateTime);
        }

        public static DateTimeOffset FromDateOnly(this DateOnly dateOnly)
        {
            return new DateTimeOffset(dateOnly.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        }
    }
}
