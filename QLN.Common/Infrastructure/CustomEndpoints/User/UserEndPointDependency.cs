using Microsoft.AspNetCore.Routing;

namespace QLN.Common.Infrastructure.CustomEndpoints.User
{
    public static class UserEndPointDependency
    {
        public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
        {
            group.MapRegisterEndpoints()
                .MapForgotPasswordEndpoint()
                .MapResetPasswordEndpoint()
                .MapLoginEndpoint()
                .MapVerify2FAEndpoint()
                .MapRefreshTokenEndpoint()
                .MapTwoFactorAuthEndpoint()
                .MapGetProfileEndpoint()
                .MapUpdateProfileEndpoint()
                .MapSendEmailOtpEndpoint()
                .MapVerifyEmailOtpEndpoint()
                .MapSendPhoneOtpEndpoint()
                .MapVerifyPhoneOtpEndpoint()
                .MapLogoutEndpoint();
            return group;
        }
    }
}
