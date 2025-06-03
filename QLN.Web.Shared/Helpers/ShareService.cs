
using QLN.Web.Shared.Model;

public class ShareService
    {
        public static string GetShareUrl(ShareRequest request)
        {
            var encodedUrl = Uri.EscapeDataString(request.UrlToShare);
            return $"https://www.facebook.com/sharer/sharer.php?u={encodedUrl}";
        }

    }

