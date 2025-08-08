using System.Text;
using System.Text.RegularExpressions;

namespace QLN.DataMigration.Helpers
{
    public static class ProcessingHelpers
    {
        public static string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;
            var slug = title.ToLowerInvariant().Trim();
            slug = Regex.Replace(slug, @"[\s_]+", "-");
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');
            return slug;
        }

        public static Guid StringToGuid(string src)
        {
            byte[] stringbytes = Encoding.UTF8.GetBytes(src);
            byte[] hashedBytes = System.Security.Cryptography.SHA1.HashData(stringbytes);
            Array.Resize(ref hashedBytes, 16);
            return new Guid(hashedBytes);
        }
    }
}
