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

        public UserLegacyData? LegacyData { get; set; }
        public LegacySubscription? LegacySubscription { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }
        public bool IsCompany { get; set; } // need to come back to this as by doing this we inherit a limitation of an existing system, but at this time "whatever" ....
    }
}
