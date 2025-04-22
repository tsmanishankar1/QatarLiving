using Microsoft.AspNetCore.Routing;

namespace QLN.Common.Infrastructure.AuthUser
{
    public static class UserEndPointDependency
    { 
        public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
        {
            group.MapRegisterEndpoints()
                .MapConfirmEmailEndpoint()
                .MapResendConfirmationEmailEndpoint()
                .MapForgotPasswordEndpoint()
                .MapResetPasswordEndpoint()
                .MapLoginEndpoint()
                .MapVerify2FAEndpoint()
                .MapRefreshTokenEndpoint()
                .MapTwoFactorAuthEndpoint()
                .MapGetProfileEndpoint()
                .MapUpdateProfileEndpoint();
            return group;
        }
    }
}
