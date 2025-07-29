using Microsoft.AspNetCore.Identity;

namespace QLN.Common.Infrastructure.Model
{
    public class ApplicationUser : IdentityUser<Guid>
    {        
        public string? MobileOperator { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Nationality { get; set; }
        public string? LanguagePreferences { get; set; }
        public string? Location { get; set; }
        public List<UserCompany>? Companies { get; set; }
        public List<UserSubscription>? Subscriptions { get; set; }
        public long? LegacyUid { get; set; }
        public List<UserLegacyData>? LegacyData { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }
    }
}
