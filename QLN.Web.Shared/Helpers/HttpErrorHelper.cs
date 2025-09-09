using MudBlazor;

namespace QLN.Web.Shared.Helpers
{
    public static class HttpErrorHelper
    {
        public static void HandleHttpException(HttpRequestException ex, ISnackbar snackbar)
        {
            var message = ParseErrorMessage(ex.Message);
            snackbar.Add(message, Severity.Error);
        }

        private static string ParseErrorMessage(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "An unexpected error occurred.";
            return raw switch
            {
                var r when r.Contains("400") => "Invalid Credentials.",
                var r when r.Contains("401") => "You are not authorized. Please log in again.",
                var r when r.Contains("403") => "Access denied. You do not have permission.",
                var r when r.Contains("404") => "Requested resource not found.",
                var r when r.Contains("409") => "This action couldn’t be completed due to a conflict. Please try again.",
                var r when r.Contains("422") => "We couldn’t process your request. Please check your input and try again.",
                var r when r.Contains("500") => "Server error occurred. Please try again later.",
                var r when r.Contains("timeout", StringComparison.OrdinalIgnoreCase) => "Request timed out.",
                _ => raw
            };
        }
    }
}