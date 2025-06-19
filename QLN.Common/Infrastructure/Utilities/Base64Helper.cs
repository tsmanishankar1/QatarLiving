using System.Text.RegularExpressions;

namespace QLN.Common.Infrastructure.Utilities
{
    public static class Base64Helper
    {
        public static (string Extension, string Base64Data) ParseBase64Image(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                throw new ArgumentException("Base64 string is null or empty.");

            if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(base64, @"^data:(?<mime>[^;]+);base64,(?<data>.+)$", RegexOptions.IgnoreCase);
                if (!match.Success)
                    throw new ArgumentException("Invalid base64 data URI format.");

                string mime = match.Groups["mime"].Value.ToLowerInvariant();
                string ext = mime switch
                {
                    "image/jpeg" => "jpg",
                    "image/png" => "png",
                    "image/jpg" => "jpg",
                    "image/svg+xml" => "svg",
                    "application/pdf" => "pdf",
                    _ => throw new NotSupportedException($"Unsupported MIME type: {mime}")
                };

                string base64Data = match.Groups["data"].Value;
                return (ext, base64Data);
            }

            byte[] bytes = Convert.FromBase64String(base64);
            string extension = GetExtensionFromMagicBytes(bytes);
            return (extension, base64);
        }

        private static string GetExtensionFromMagicBytes(byte[] bytes)
        {
            if (bytes.Length < 4) return "bin";
            if (bytes[0] == 0xFF && bytes[1] == 0xD8) return "jpg";
            if (bytes[0] == 0x89 && bytes[1] == 0x50) return "png";
            if (bytes[0] == 0x47 && bytes[1] == 0x49) return "gif";
            if (bytes[0] == 0x42 && bytes[1] == 0x4D) return "bmp";
            return "bin";
        }
    }
}

