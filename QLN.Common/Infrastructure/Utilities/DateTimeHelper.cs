using System;

namespace QLN.Common.Infrastructure.Utilities
{
    public static class DateTimeHelper
    {
        /// <summary>
        /// DateTime? -> DateTimeOffset? (assumes UTC if Kind is Unspecified).
        /// </summary>
        public static DateTimeOffset? ToDateTimeOffset(this DateTime? dateTime)
        {
            if (!dateTime.HasValue) return null;

            var dt = dateTime.Value;
            return dt.Kind == DateTimeKind.Unspecified
                ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc))
                : new DateTimeOffset(dt);
        }

        /// <summary>
        /// DateTime -> DateTimeOffset (assumes UTC if Kind is Unspecified).
        /// </summary>
        public static DateTimeOffset ToDateTimeOffset(this DateTime dateTime)
        {
            return dateTime.Kind == DateTimeKind.Unspecified
                ? new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc))
                : new DateTimeOffset(dateTime);
        }

        /// <summary>
        /// DateOnly -> DateTimeOffset at 00:00:00 UTC.
        /// </summary>
        public static DateTimeOffset FromDateOnly(this DateOnly dateOnly)
        {
            return new DateTimeOffset(dateOnly.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        }

        /// <summary>
        /// DateTimeOffset -> DateOnly (uses UTC date).
        /// </summary>
        public static DateOnly ToDateOnly(this DateTimeOffset dto)
        {
            return DateOnly.FromDateTime(dto.UtcDateTime);
        }

        /// <summary>
        /// DateTimeOffset? -> DateOnly? (uses UTC date).
        /// </summary>
        public static DateOnly? ToDateOnly(this DateTimeOffset? dto)
        {
            return dto.HasValue ? DateOnly.FromDateTime(dto.Value.UtcDateTime) : (DateOnly?)null;
        }

        /// <summary>
        /// DateTime -> DateOnly (uses UTC date; assumes UTC if Unspecified).
        /// </summary>
        public static DateOnly ToDateOnly(this DateTime dt)
        {
            var utc = dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime();
            return DateOnly.FromDateTime(utc);
        }

        /// <summary>
        /// DateOnly -> DateTime (UTC midnight).
        /// </summary>
        public static DateTime ToUtcDateTime(this DateOnly date)
        {
            return DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        }
    }
}
