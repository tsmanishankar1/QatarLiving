using System.Text.RegularExpressions;

namespace QLN.Blazor.Base.Helpers
{
    public static class ValidationHelper
    {
      public static bool IsValidPhoneNumber(string phoneNumber)
{
    if (string.IsNullOrWhiteSpace(phoneNumber))
        return false;

    // Remove hyphens to check the digit count
    var digitsOnly = phoneNumber.Replace("-", "");

    if (!Regex.IsMatch(digitsOnly, @"^\d{6,15}$"))
        return false;

    // Ensure the original input only contains digits and hyphens
    return Regex.IsMatch(phoneNumber, @"^[\d-]+$");
}

        public static bool IsValidEmail(string email)
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                // Updated regular expression to prevent consecutive dots anywhere in the email
                var regex = new Regex(@"^(?!.*\.\.)[^@\s]+@[^@\s]+\.[^@\s]+$");

                return regex.IsMatch(email);
            }


        public static string ValidatePassword(string password)
        {
           
            if (string.IsNullOrWhiteSpace(password))
            {
                return "Please enter your password.";
            }
            else if (password.Length < 8)
            {
                return "Password must be at least 8 characters long.";
            }
            else if (!password.Any(char.IsDigit))
            {
                return "Password must contain at least one digit.";
            }
            else if (!password.Any(char.IsLower))
            {
                return "Password must contain at least one lowercase letter.";
            }
            else if (!password.Any(char.IsUpper))
            {
                return "Password must contain at least one uppercase letter.";
            }
            else if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                return "Password must contain at least one special character.";
            }

            return string.Empty; 
        }
    }
}
