using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.AuthUser
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
