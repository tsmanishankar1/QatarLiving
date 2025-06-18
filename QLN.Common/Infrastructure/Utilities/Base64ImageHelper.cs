using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Utilities
{
public static class Base64ImageHelper
{
    public static (string Extension, string Base64Data) ParseBase64Image(string base64)
    {
        if (base64.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(base64, @"^data:image/(?<type>[a-zA-Z0-9\+\-]+);base64,(?<data>.+)$");
            if (!match.Success)
                throw new ArgumentException("Invalid base64 image format.");

            string ext = match.Groups["type"].Value switch
            {
                "jpeg" => "jpg",
                    "image/png" => "png",
                "svg+xml" => "svg",
                    "application/pdf" => "pdf",
                var type => type
            };
            string base64Data = match.Groups["data"].Value;
            return (ext, base64Data);
        }

        // No prefix: auto-detect extension from magic bytes
        byte[] bytes = Convert.FromBase64String(base64);
        string extension = GetExtensionFromMagicBytes(bytes);

        // Optionally: if extension is "bin", you may want to throw or default to png/jpg
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

    // Utility to reconstruct data URL for FE preview (optional)
    public static string ToDataUrl(string extension, string base64)
    {
        // Handle svg case
        if (extension == "svg") extension = "svg+xml";
        return $"data:image/{extension};base64,{base64}";
    }

        public static (string Extension, string Base64Data) ParsePdfFile(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                throw new ArgumentException("Input base64 string is null or empty.");

            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                string extension = GetPdfExtensionFromMagicBytes(bytes);

                if (extension != "pdf")
                    throw new NotSupportedException("Only PDF format is supported for certificates.");

                return (extension, base64);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid base64 string format.");
            }
        }

        private static string GetPdfExtensionFromMagicBytes(byte[] bytes)
        {
            if (bytes.Length < 4) return "bin";

            // PDF: 25 50 44 46 (%PDF)
            if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
                return "pdf";

            return "bin";
        }


    }
}
