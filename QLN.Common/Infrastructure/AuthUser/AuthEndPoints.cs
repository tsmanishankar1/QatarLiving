using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.Service;
using static QLN.Common.Infrastructure.DTO_s.OtpDTO;


namespace QLN.Common.Infrastructure.AuthUser
{
    public static class AuthEndPoints
    {
        // register
        public static RouteGroupBuilder MapRegisterEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/register", async Task<Results<Ok<ApiResponse<string>>, ValidationProblem>>
            (
                [FromBody] RegisterRequest request,
                HttpContext context,
                [FromServices] IAuthService authService // Inject only the service now
            ) =>
            {
                // Delegate to service method
                return await authService.RegisterAsync(request, context);
            })
            .WithName("Register")
            .WithTags("Authentication")
            .WithSummary("Register a new user")
            .WithDescription("Registers a new user and sends an email confirmation link.");

            return group;
        }

        // Email OTP Send
        public static RouteGroupBuilder MapSendEmailOtpEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/verify-email-request", async Task<Ok<ApiResponse<string>>> (
                [FromBody] EmailOtpRequest request,
                [FromServices] IAuthService authService
            ) =>
            {
                return await authService.SendEmailOtpAsync(request.Email);
            })
            .WithName("SendEmailOtp")
            .WithTags("Authentication")
            .WithSummary("Send Email OTP")
            .WithDescription("Sends an OTP to the user's email address for verification.");

            return group;
        }

        // Email OTP Verify
        public static RouteGroupBuilder MapVerifyEmailOtpEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/verify-email-otp", async Task<Results<Ok<ApiResponse<string>>, BadRequest<string>>> (
                [FromBody] VerifyEmailOtpRequest request,
                [FromServices] IAuthService authService
            ) =>
            {
                return await authService.VerifyEmailOtpAsync(request.Email, request.Otp);
            })
            .WithName("VerifyEmailOtp")
            .WithTags("Authentication")
            .WithSummary("Verify Email OTP")
            .WithDescription("Verifies the OTP entered by the user for their email address.");

            return group;
        }

        // Phone OTP Send
        public static RouteGroupBuilder MapSendPhoneOtpEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/verify-phone-request", async Task<Ok<ApiResponse<string>>> (
                [FromBody] PhoneOtpRequest request,
                [FromServices] IAuthService authService
            ) =>
            {
                return await authService.SendPhoneOtpAsync(request.PhoneNumber);
            })
            .WithName("SendPhoneOtp")
            .WithTags("Authentication")
            .WithSummary("Send Phone OTP")
            .WithDescription("Generates and stores OTP for phone number verification.");

            return group;
        }

        // Phone OTP Verify
        public static RouteGroupBuilder MapVerifyPhoneOtpEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/verify-phone-otp", async Task<Results<Ok<ApiResponse<string>>, BadRequest<string>>> (
                [FromBody] VerifyPhoneOtpRequest request,
                [FromServices] IAuthService authService
            ) =>
            {
                return await authService.VerifyPhoneOtpAsync(request.PhoneNumber, request.Otp);
            })
            .WithName("VerifyPhoneOtp")
            .WithTags("Authentication")
            .WithSummary("Verify Phone OTP")
            .WithDescription("Verifies the OTP entered for the phone number.");

            return group;
        }
      
        // Forgot Password
        public static RouteGroupBuilder MapForgotPasswordEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/forgot-password", async Task<Ok<ApiResponse<string>>> (
                [FromBody] ForgotPasswordRequest request,
                [FromServices] IAuthService authService
            ) =>
            {
                return await authService.ForgotPasswordAsync(request);
            })
            .WithName("ForgotPassword")
            .WithTags("Authentication")
            .WithSummary("Request password reset")
            .WithDescription("Sends a password reset link to the user’s email if the email is registered and confirmed.");

            return group;
        }

        // Reset Password
        public static RouteGroupBuilder MapResetPasswordEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/reset-password", async Task<Results<Ok<ApiResponse<string>>, ValidationProblem>> (
                [FromBody] ResetPasswordRequest request,
                [FromServices] IAuthService authService
            ) =>
            {
                return await authService.ResetPasswordAsync(request);
            })
            .WithName("ResetPassword")
            .WithTags("Authentication")
            .WithSummary("Reset password")
            .WithDescription("Resets the user’s password using a valid reset token.");

            return group;
        }

        // login              
        public static RouteGroupBuilder MapLoginEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/login", async Task<Results<Ok<ApiResponse<LoginResponse>>, UnauthorizedHttpResult, ValidationProblem>> (
                [FromBody] LoginRequest request,
                [FromServices] IAuthService authService
            ) =>
            {
                return await authService.LoginAsync(request);
            })
            .WithName("Login")
            .WithTags("Authentication")
            .WithSummary("User login")
            .WithDescription("Logs in a user using email/username/phone and password. If 2FA is enabled, a verification code is sent to the email.");

            return group;
        }


        // verify twofactorAuth
        public static RouteGroupBuilder MapVerify2FAEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/verify-2fa", async Task<Results<Ok<ApiResponse<LoginResponse>>, ValidationProblem, NotFound>> (
                [FromBody] Verify2FARequest request,
                [FromServices] IAuthService authService
            ) =>
            {
                return await authService.Verify2FAAsync(request);
            })
            .WithName("VerifyTwoFactor")
            .WithTags("Authentication")
            .WithSummary("Verify Two-Factor Authentication")
            .WithDescription("Verifies the OTP code sent to the user's email and completes login by issuing tokens.");

            return group;
        }


        // refresh
        public static RouteGroupBuilder MapRefreshTokenEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/refresh", async Task<Results<Ok<ApiResponse<RefreshTokenResponse>>, UnauthorizedHttpResult>> (
                [FromBody] RefreshTokenRequest request,
                [FromServices] IAuthService authService
            ) =>
            {
                return await authService.RefreshTokenAsync(request);
            })
            .WithName("RefreshToken")
            .WithTags("Authentication")
            .WithSummary("Refresh JWT access token")
            .WithDescription("Uses a valid refresh token to generate new access and refresh tokens.");

            return group;
        }


        // twofactorEnabled
        public static RouteGroupBuilder MapTwoFactorAuthEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/manage/2fa", async Task<IResult> (
                [FromBody] TwoFactorToggleRequest request,
                [FromServices] IAuthService authService
            ) =>
            {
                return await authService.Toggle2FAAsync(request);
            })
            .WithName("ToggleTwoFactor")
            .WithTags("Manage Account")
            .WithSummary("Enable or disable two-factor authentication")
            .WithDescription("Allows user to enable or disable 2FA.");

            return group;
        }


        // get manageinfo
        public static RouteGroupBuilder MapGetProfileEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/manage/info", async Task<IResult> (
                [FromQuery] string identity,
                [FromServices] IAuthService authService
            ) =>
            {
                return await authService.GetProfileAsync(identity);
            })
            .WithName("GetProfile")
            .WithTags("Manage Account")
            .WithSummary("Get user profile information")
            .WithDescription("Retrieves profile data using email as identity.");

            return group;
        }


        // post manageinfo (update profile)
        public static RouteGroupBuilder MapUpdateProfileEndpoint(this RouteGroupBuilder group)
        {
            group.MapPut("/manage/update", async Task<IResult> (
                [FromBody] UpdateProfileRequest request,
                [FromServices] IAuthService authService
            ) =>
            {
                return await authService.UpdateProfileAsync(request);
            })
            .WithName("UpdateProfile")
            .WithTags("Manage Account")
            .WithSummary("Update user profile")
            .WithDescription("Allows a user to update their profile details.");

            return group;
        }

       

    }
}
