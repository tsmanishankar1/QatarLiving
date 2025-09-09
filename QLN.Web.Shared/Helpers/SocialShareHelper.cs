using Microsoft.JSInterop;

namespace QLN.Web.Shared.Helpers
{
    public static class SocialShareHelper
    {
        public static string GetFacebookUrl(string currentUrl) =>
            $"https://www.facebook.com/sharer/sharer.php?u={Uri.EscapeDataString(currentUrl)}";

        public static string GetWhatsAppUrl(string currentUrl) =>
            $"https://wa.me/?text={Uri.EscapeDataString(currentUrl)}";

        public static string GetInstagramUrl(string currentUrl)
        {
            // Instagram does not support direct URL sharing via link
            // We'll return the profile page as fallback
            return "https://www.instagram.com/";
        }

        public static string GetTikTokUrl(string currentUrl)
        {
            // TikTok does not support direct URL sharing via link
            // We'll return the main page as fallback
            return "https://www.tiktok.com/";
        }

        public static string GetXUrl(string currentUrl, string text = "")
        {
            // X (formerly Twitter) uses the same sharing endpoint as Twitter
            // Fallback to Twitter sharing if X-specific is unavailable
            return $"https://twitter.com/intent/tweet?url={Uri.EscapeDataString(currentUrl)}&text={Uri.EscapeDataString(text)}";
        }

        public static string GetLinkedInUrl(string currentUrl, string title = "", string summary = "")
        {
            // LinkedIn sharing with optional title and summary
            // If title/summary not provided, only URL will be shared
            return $"https://www.linkedin.com/sharing/share-offsite/?url={Uri.EscapeDataString(currentUrl)}"
                + (string.IsNullOrWhiteSpace(title) ? "" : $"&title={Uri.EscapeDataString(title)}")
                + (string.IsNullOrWhiteSpace(summary) ? "" : $"&summary={Uri.EscapeDataString(summary)}");
        }

        public static async Task<bool> CopyLinkToClipboardAsync(IJSRuntime jsRuntime, string currentUrl)
        {
            try
            {
                await jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", currentUrl);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}