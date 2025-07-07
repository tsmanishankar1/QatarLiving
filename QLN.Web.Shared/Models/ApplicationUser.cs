using Microsoft.AspNetCore.Identity;

namespace QLN.Web.Shared.Model
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? MobileOperator { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateOnly DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string Nationality { get; set; } = null!;
        public string? LanguagePreferences { get; set; }
        public string? Location { get; set; }
        public bool IsCompany { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = null;
    }
}
