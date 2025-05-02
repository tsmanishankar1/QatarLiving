using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using Microsoft.AspNetCore.Builder;
using static QLN.Common.Infrastructure.DTO_s.OtpDTO;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.IAuthService;
using QLN.Common.Infrastructure.Utilities;
using QLN.Common.Infrastructure.DTO_s;


namespace QLN.Common.Infrastructure.CustomEndpoints.User
{
    public static class AuthEndPoints
    {
        // register
        public static RouteGroupBuilder MapRegisterEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/register", async Task<Results<
                Ok<ApiResponse<string>>,
                BadRequest<ApiResponse<string>>,
                ValidationProblem,
                NotFound<ApiResponse<string>>,
                Conflict<ApiResponse<string>>,
                ProblemHttpResult>>
            (
                [FromBody] RegisterRequest request,
                HttpContext context,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {

                    var result = await authService.Register(request, context);
                    return result;
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("Register")
            .WithTags("Authentication")
            .WithSummary("Register a new user")
            .WithDescription("Registers a new user and sends an email confirmation link.")
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<string>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<string>>(StatusCodes.Status409Conflict)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            return group;
        }

        // Email OTP Send
        public static RouteGroupBuilder MapSendEmailOtpEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/verify-email-request", async (
                [FromBody] EmailOtpRequest request,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    return await authService.SendEmailOtp(request.Email);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("SendEmailOtp")
            .WithTags("Authentication")
            .WithSummary("Send Email OTP")
            .WithDescription("Sends an OTP to the user's email address for verification.")
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<string>>(StatusCodes.Status409Conflict)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            return group;
        }

        // Email OTP Verify
        public static RouteGroupBuilder MapVerifyEmailOtpEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/verify-email-otp", async (
                [FromBody] VerifyEmailOtpRequest request,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    return await authService.VerifyEmailOtp(request.Email, request.Otp);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                    detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                    statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("VerifyEmailOtp")
            .WithTags("Authentication")
            .WithSummary("Verify Email OTP")
            .WithDescription("Verifies the OTP entered by the user for their email address.")
             .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
             .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

            return group;
        }

        // Phone OTP Send
        public static RouteGroupBuilder MapSendPhoneOtpEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/verify-phone-request", async Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, BadRequest<ApiResponse<string>>>> (
                [FromBody] PhoneOtpRequest request,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    var result = await authService.SendPhoneOtp(request.PhoneNumber);
                    return result;
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        detail: "An unexpected error occurred. Please try again later.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("SendPhoneOtp")
            .WithTags("Authentication")
            .WithSummary("Send Phone OTP")
            .WithDescription("Generates and stores OTP for phone number verification. If the phone number is already registered, it will return a failure response.")
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            return group;
        }


        // Phone OTP Verify
        public static RouteGroupBuilder MapVerifyPhoneOtpEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/verify-phone-otp", async Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, BadRequest<ApiResponse<string>>>> (
                [FromBody] VerifyPhoneOtpRequest request,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    return await authService.VerifyPhoneOtp(request.PhoneNumber, request.Otp);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("VerifyPhoneOtp")
            .WithTags("Authentication")
            .WithSummary("Verify Phone OTP")
            .WithDescription("Verifies the OTP entered for the phone number.")
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            return group;
        }

        // Forgot Password
        public static RouteGroupBuilder MapForgotPasswordEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/forgot-password", async (
                [FromBody] ForgotPasswordRequest request,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    return await authService.ForgotPassword(request);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("ForgotPassword")
            .WithTags("Authentication")
            .WithSummary("Request password reset")
            .WithDescription("Sends a password reset link to the user’s email if the email is registered and confirmed.")
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            return group;
        }

        // Reset Password
        public static RouteGroupBuilder MapResetPasswordEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/reset-password", async (
                [FromBody] ResetPasswordRequest request,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    return await authService.ResetPassword(request);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("ResetPassword")
                .WithTags("Authentication")
                .WithSummary("Reset password")
                .WithDescription("Resets the user’s password using a valid reset token.")
                .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
                .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            return group;
        }

        // login              
        public static RouteGroupBuilder MapLoginEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/login", async Task<Results<
                Ok<ApiResponse<LoginResponse>>,
                BadRequest<ApiResponse<string>>,
                UnauthorizedHttpResult,
                ProblemHttpResult,
                ValidationProblem>> (
                [FromBody] LoginRequest request,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    return await authService.Login(request);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("Login")
            .WithTags("Authentication")
            .WithSummary("User login")
            .WithDescription("Logs in a user using email/username/phone and password. If 2FA is enabled, a verification code is sent to the email.")
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<string>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<string>>(StatusCodes.Status409Conflict)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            return group;
        }

        // Send 2FA OTP (for login or resend)
        public static RouteGroupBuilder MapSend2FAOtpEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/send-2fa", async Task<Results<
                Ok<ApiResponse<string>>,
                BadRequest<ApiResponse<string>>,
                ProblemHttpResult>> (
                [FromBody] Send2FARequest request,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    var result = await authService.SendTwoFactorOtp(request);
                    return result.Status
                        ? TypedResults.Ok(result)
                        : TypedResults.BadRequest(result);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        detail: ApiResponse<string>.Fail("An unexpected error occurred.").Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("Send2FAOtp")
            .WithTags("Authentication")
            .WithSummary("Send 2FA OTP")
            .WithDescription("Sends a two-factor authentication OTP using email, phone, or authenticator.")
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            return group;
        }

        // verify twofactorAuth
        public static RouteGroupBuilder MapVerify2FAEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/verify-2fa", async Task<Results<Ok<ApiResponse<LoginResponse>>, BadRequest<ApiResponse<string>>, ProblemHttpResult, ValidationProblem>> (
                [FromBody] Verify2FARequest request,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    return await authService.Verify2FA(request);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("VerifyTwoFactor")
            .WithTags("Authentication")
            .WithSummary("Verify Two-Factor Authentication")
            .WithDescription("Verifies the OTP code sent to the user's email and completes login by issuing tokens.")
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<string>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<string>>(StatusCodes.Status409Conflict)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            return group;
        }

        // refresh
        public static RouteGroupBuilder MapRefreshTokenEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/refresh", async Task<Results<Ok<ApiResponse<RefreshTokenResponse>>, BadRequest<ApiResponse<string>>, ProblemHttpResult, UnauthorizedHttpResult>> (
                [FromBody] RefreshTokenRequest request,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    return await authService.RefreshToken(request);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("RefreshToken")
            .WithTags("Authentication")
            .WithSummary("Refresh JWT access token")
            .WithDescription("Uses a valid refresh token to generate new access and refresh tokens.")
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<string>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<string>>(StatusCodes.Status409Conflict)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            return group;
        }


        // twofactorEnabled
        public static RouteGroupBuilder MapTwoFactorAuthEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/manage/2fa", async Task<IResult> (
                [FromBody] TwoFactorToggleRequest request,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    return await authService.Toggle2FA(request);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("ToggleTwoFactor")
            .WithTags("Manage Account")
            .WithSummary("Enable or disable two-factor authentication")
            .WithDescription("Allows user to enable or disable 2FA.")
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<string>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<string>>(StatusCodes.Status409Conflict)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            return group;
        }


        // get manageinfo
        public static RouteGroupBuilder MapGetProfileEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/manage/info", async Task<IResult> (
                HttpContext context,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log

            ) =>
            {
                try
                {
                    var userId = context.User.GetId();
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.Unauthorized();
                    }
                    return await authService.GetProfile(userId);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetProfile")
            .WithTags("Manage Account")
            .WithSummary("Get user profile information")
            .WithDescription("Retrieves profile data using email as identity.")
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<string>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<string>>(StatusCodes.Status409Conflict)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            return group;
        }


        // post manageinfo (update profile)
        public static RouteGroupBuilder MapUpdateProfileEndpoint(this RouteGroupBuilder group)
        {
            group.MapPut("/manage/update", async Task<IResult> (
                HttpContext context,
                [FromBody] UpdateProfileRequest request,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    var userId = context.User.GetId();
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.Unauthorized();
                    }
                    return await authService.UpdateProfile(userId, request);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("UpdateProfile")
            .WithTags("Manage Account")
            .WithSummary("Update user profile")
            .WithDescription("Allows a user to update their profile details.")
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            return group;
        }

        // log out endpoint
        public static RouteGroupBuilder MapLogoutEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/logout", async (
                HttpContext context,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    var userId = context.User.GetId();
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.Unauthorized();
                    }
                    return await authService.Logout(userId);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        detail: ApiResponse<string>.Fail("An unexpected error occurred during logout.").Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("Logout")
            .WithTags("Authentication")
            .WithSummary("Logs out the user")
            .WithDescription("Removes tokens and signs the user out.")
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<string>>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            return group;
        }

    }
}
