using QLN.Common.Infrastructure.Model;

namespace QLN.Common.Infrastructure.IService.ITokenService
{
    public interface ITokenService
    {
        Task<string> GenerateAccessToken(ApplicationUser user);
        string GenerateRefreshToken();
    }
}
