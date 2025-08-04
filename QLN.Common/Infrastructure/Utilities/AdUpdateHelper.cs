using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Utilities
{
    public static class AdUpdateHelper
    {
        public static readonly HashSet<string> ExcludedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "IsRefreshed",
        "IsPromoted",
        "IsFeatured",
        "LastRefreshedOn",
        "PromotedExpiryDate",
        "FeaturedExpiryDate",
        "PublishedDate",
        "ExpiryDate",
        "SubscriptionId",
        "CreatedAt",
        "CreatedBy",
        "IsActive",
        "AdType",
        "SubVertical",
        "UserName",
        "UserId"
    };

        public static void ApplySelectiveUpdates<T>(T existing, T updated)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (ExcludedProperties.Contains(prop.Name) || !prop.CanWrite)
                    continue;

                var oldValue = prop.GetValue(existing);
                var newValue = prop.GetValue(updated);

                if (!Equals(oldValue, newValue))
                {
                    prop.SetValue(existing, newValue);
                }
            }
        }
    }

}
