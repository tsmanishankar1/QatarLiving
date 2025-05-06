namespace QLN.Web.Shared.Models;
public class UserState
{
    public string? Email { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? mobileNumber { get; set; }
    public string? username { get; set; }
    public string? Token { get; set; }

    public bool IsLoggedIn => !string.IsNullOrEmpty(Email);
}
