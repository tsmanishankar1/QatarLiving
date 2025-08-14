using System.Text.RegularExpressions;

namespace QLN.Common.Infrastructure.Utilities
{
    public static class SlugHelper
    {
        public static string GenerateSlug(string? verticalName, string? categoryName, string? title, Guid? id)
        {
            var combined = string.Join("-", new[] { verticalName, categoryName, title }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p!.Trim()));

            if (string.IsNullOrWhiteSpace(combined) && id == null)
                return string.Empty;

            var slug = combined.ToLowerInvariant();
            slug = Regex.Replace(slug, @"[\s_]+", "-");  
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", ""); 
            slug = Regex.Replace(slug, @"-+", "-");      
            slug = slug.Trim('-');

            if (id.HasValue && id.Value != Guid.Empty)
            {
                slug = $"{slug}-{id.Value.ToString("N")[..8]}";
            }

            return slug;
        }
    }
}
