using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
public class DailyNewsLetterBase : ComponentBase
{
    public string Email { get; set; }
    public bool IsLoading { get; set; } = false;
    public string ErrorMessage { get; set; }

    public void  HandleResetPassword()
    {
       ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Email is required.";
            return;
        }

        if (!IsValidEmail(Email))
        {
            ErrorMessage = "Please enter a valid email address.";
            return;
        }

        IsLoading = true;

        try
        {
            Email = string.Empty;
        }
        finally
        {
            IsLoading = false;
        }
     
    }
     public bool IsValidEmail(string email)
    {
        var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return System.Text.RegularExpressions.Regex.IsMatch(email, pattern);
    }

}