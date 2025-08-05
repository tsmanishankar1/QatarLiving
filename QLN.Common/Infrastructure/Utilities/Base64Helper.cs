using System.Text;
using System.Text.RegularExpressions;

namespace QLN.Common.Infrastructure.Utilities
{
    public static class Base64Helper
    {
        public static (string Extension, string Base64Data) ParseBase64(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                throw new ArgumentException("Base64 string is null or empty.");

            if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(base64, @"^data:(?<mime>[\w/\-\+\.]+);base64,(?<data>.+)$");
                if (!match.Success)
                    throw new ArgumentException("Invalid base64 format with data URI.");

                string mime = match.Groups["mime"].Value;
                string data = match.Groups["data"].Value;

                string ext = mime switch
                {
                    "image/jpeg" or "image/jpg" => "jpg",
                    "image/png" => "png",
                    "image/gif" => "gif",
                    "image/bmp" => "bmp",
                    "image/svg+xml" => "svg",
                    "image/webp" => "webp",
                    "image/heic" => "heic",
                    "application/pdf" => "pdf",
                    "application/xml" => "xml",
                    "text/xml" => "xml",
                    _ => throw new ArgumentException($"Unsupported MIME type: {mime}")
                };

                return (ext, data);
            }
            else
            {
                return ParseBase64Raw(base64);
            }
        }

        public static (string Extension, string Base64Data) ParseBase64Raw(string base64)
        {
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid base64 string format.");
            }

            string ext = GetExtensionFromMagicBytes(bytes);
            if (ext == "bin")
                throw new ArgumentException("Unknown file format. Supported: jpg, png, pdf.");

            return (ext, base64);
        }

        private static string GetExtensionFromMagicBytes(byte[] bytes)
        {
            if (bytes.Length < 12) return "bin";
            if (bytes[0] == 0xFF && bytes[1] == 0xD8) return "jpg";
            if (bytes[0] == 0x89 && bytes[1] == 0x50) return "png";
            if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46) return "pdf";
            if (bytes[0] == 0x47 && bytes[1] == 0x49) return "gif";
            if (bytes[0] == 0x42 && bytes[1] == 0x4D) return "bmp";
            var brand = Encoding.ASCII.GetString(bytes.Skip(4).Take(8).ToArray());
            if (brand.StartsWith("ftypheic") || brand.StartsWith("ftypheix"))
                return "heic";
            if (Encoding.ASCII.GetString(bytes.Take(4).ToArray()) == "RIFF" &&
                Encoding.ASCII.GetString(bytes.Skip(8).Take(4).ToArray()) == "WEBP")
                return "webp";
            var headerString = Encoding.ASCII.GetString(bytes.Take(5).ToArray());
            if (headerString == "<?xml")
                return "xml";
            return "bin";
        }
    }
}
