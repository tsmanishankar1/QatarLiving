using System.Text.RegularExpressions;

namespace QLN.Blazor.Base.Helpers
{
    public static class ValidationHelper
    {
        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            var regex = new Regex(@"^\d{6,15}$");
            return regex.IsMatch(phoneNumber);
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return regex.IsMatch(email);
        }
    }
}
