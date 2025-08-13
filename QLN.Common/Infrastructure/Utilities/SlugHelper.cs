using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Utilities
{
    public static class SlugHelper
    {
        public static string GenerateSlug(string? verticalName, string? categoryName, string? title)
        {
            var combined = string.Join("-", new[] { verticalName, categoryName, title }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p!.Trim()));

            if (string.IsNullOrWhiteSpace(combined))
                return string.Empty;

            var slug = combined.ToLowerInvariant();
            slug = Regex.Replace(slug, @"[\s_]+", "-");
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');

            return slug;
        }
    }
}
