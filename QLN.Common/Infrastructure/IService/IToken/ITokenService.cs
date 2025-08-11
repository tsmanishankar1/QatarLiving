using QLN.Common.Infrastructure.Model;

namespace QLN.Common.Infrastructure.IService.ITokenService
{
    public interface ITokenService
    {
        Task<string> GenerateAccessToken(ApplicationUser user);
        Task<string> GenerateEnrichedAccessToken(ApplicationUser user, DrupalUser drupalUser, DateTime expiry, IList<string>? roles);
        string GenerateRefreshToken();
    }
}
