using QLN.Common.Indexing.IndexModels;
using QLN.Common.Infrastructure.InputModels;

namespace QLN.Common.Infrastructure.ServiceInterface
{
    public interface IAuthService
    {
        Task<string> AddUserProfileAsync(UserProfileCreateRequest request);
        Task<string> VerifyOtpAsync(AccountVerification request);
        Task<string> RequestOtp(string name);
        Task<LoginResponse> VerifyUserLogin(string name, string passwordOrOtp);
        Task<string> RefreshTokenAsync(string oldRefreshToken);
        Task<List<UserIndex>> SearchUsersFromIndexAsync(string? query);
    }
}
