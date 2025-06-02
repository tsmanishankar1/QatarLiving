using Microsoft.AspNetCore.Components;
using System;

namespace QLN.Web.Shared.Helpers
{
    public static class SocialShareHelper
    {
        public static string GetFacebookUrl(string currentUrl) =>
            $"https://www.facebook.com/sharer/sharer.php?u={Uri.EscapeDataString(currentUrl)}";

        public static string GetWhatsAppUrl(string currentUrl) =>
            $"https://wa.me/?text={Uri.EscapeDataString(currentUrl)}";

        public static string GetTwitterUrl(string currentUrl, string text = "") =>
            $"https://twitter.com/intent/tweet?url={Uri.EscapeDataString(currentUrl)}&text={Uri.EscapeDataString(text)}";

        public static string GetInstagramUrl(string currentUrl)
        {
            // Instagram does not support direct URL sharing via link
            // We'll return the profile page as fallback (optional)
            return "https://www.instagram.com/";
        }
    }
}
