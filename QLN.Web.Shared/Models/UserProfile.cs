namespace QLN.Web.Shared.Models
{
    public class UserProfile
{
    public string UserName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string LanguagePreferences { get; set; }
    public string Nationality { get; set; }
    public string MobileOperator { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsCompany { get; set; }
    public bool TwoFactorEnabled { get; set; }
}
}
