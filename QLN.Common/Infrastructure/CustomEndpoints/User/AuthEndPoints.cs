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
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.Constants;


namespace QLN.Common.Infrastructure.CustomEndpoints.User
{
    public static class AuthEndPoints
    {
        /// register
        public static RouteGroupBuilder MapRegisterEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/register", async Task<Results<Ok<string>,
              BadRequest<ProblemDetails>,
             Conflict<ProblemDetails>,
             NotFound<ProblemDetails>,
             ValidationProblem,
             ProblemHttpResult>>
          ( [FromBody] RegisterRequest request,
            HttpContext context,
            [FromServices] IAuthService authService,
            [FromServices] IEventlogger log) =>
            {
                try
                {
                    var result = await authService.Register(request, context);
                    return TypedResults.Ok(result); 
                }
                catch (VerificationRequiredException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Verification Required",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }
                catch (EmailAlreadyRegisteredException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Email Already Registered",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict,
                        Instance = context.Request.Path
                    });
                }
                catch (UsernameTakenException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Username Already Taken",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict,
                        Instance = context.Request.Path
                    });
                }
                catch (InvalidMobileFormatException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Mobile Number Format",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }
                catch (InvalidEmailFormatException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Email Format",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }
                catch (RegistrationValidationException ex)
                {
                    return TypedResults.ValidationProblem(ex.Errors);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred.",
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
            .WithName("Register")
            .WithTags("Authentication")
            .WithSummary("Register a new user")
            .WithDescription("Registers a new user and sends a confirmation link or OTP based on the verification method.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }

        /// Email OTP Send
        public static RouteGroupBuilder MapSendEmailOtpEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/verify-email-request", async (
                [FromBody] EmailOtpRequest request,
                HttpContext context,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    return await authService.SendEmailOtp(request.Email);
                }
                catch (InvalidEmailFormatException ex)
                {
                    return Results.Problem(
                        title: "Invalid Email",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status400BadRequest,
                        instance: context.Request.Path
                    );
                }
                catch (EmailAlreadyRegisteredException ex)
                {
                    return Results.Problem(
                        title: "Email Already Exists",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status409Conflict,
                        instance: context.Request.Path
                    );
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return Results.Problem(
                        detail: "An unexpected error occurred. Please try again later.",
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path
                    );
                }
            })
            .WithName("SendEmailOtp")
            .WithTags("Authentication")
            .WithSummary("Send Email OTP")
            .WithDescription("Sends an OTP to the user's email address for verification.")
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            return group;
        }

        /// Email OTP Verify
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
                        detail: "An unexpected error occurred. Please try again later.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("VerifyEmailOtp")
            .WithTags("Authentication")
            .WithSummary("Verify Email OTP")
            .WithDescription("Verifies the OTP entered by the user for their email address.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status400BadRequest)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }


        /// Phone OTP Send
        public static RouteGroupBuilder MapSendPhoneOtpEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/verify-phone-request", async Task<Results<
            Ok<string>,
            ProblemHttpResult,
            Conflict<ProblemDetails>>> (
            [FromBody] PhoneOtpRequest request,
            [FromServices] IAuthService authService,
            [FromServices] IEventlogger log
              ) =>
            {
                try
                {
                    return await authService.SendPhoneOtp(request.PhoneNumber);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred. Please try again later.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
           .WithName("SendPhoneOtp")
           .WithTags("Authentication")
           .WithSummary("Send Phone OTP")
           .WithDescription("Sends OTP to the given phone number if it's not already registered.")
           .Produces<string>(StatusCodes.Status200OK)
           .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        /// Phone OTP Verify
        public static RouteGroupBuilder MapVerifyPhoneOtpEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/verify-phone-otp", async Task<Results<Ok<string>,
            ProblemHttpResult,
            BadRequest<ProblemDetails>>> (
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
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred. Please try again later.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("VerifyPhoneOtp")
            .WithTags("Authentication")
            .WithSummary("Verify Phone OTP")
            .WithDescription("Verifies the OTP entered by the user for their phone number.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        /// Forgot Password
        public static RouteGroupBuilder MapForgotPasswordEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/forgot-password", async Task<Results<Ok<string>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>> (
           [FromBody] ForgotPasswordRequest request,
           [FromServices] IAuthService authService,
           [FromServices] IEventlogger log) =>
            {
                try
                {
                    return await authService.ForgotPassword(request);
                }
                catch (UserNotFoundException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Email",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred. Please try again later.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("ForgotPassword")
            .WithTags("Authentication")
            .WithSummary("Request password reset")
            .WithDescription("Sends a password reset link to the user’s email if the email is registered and confirmed.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        // Reset Password
        public static RouteGroupBuilder MapResetPasswordEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/reset-password", async Task<Results<Ok<string>,
            BadRequest<ProblemDetails>,
            NotFound<ProblemDetails>,
            ValidationProblem,
            ProblemHttpResult>>
           (
             [FromBody] ResetPasswordRequest request,
             HttpContext context,
            [FromServices] IAuthService authService,
            [FromServices] IEventlogger log
           ) =>
            {
                try
                {
                    return await authService.ResetPassword(request);
                }
                catch (InvalidTokenException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid or Expired Token",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }
                catch (UserNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "User Not Found or Not Confirmed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound,
                        Instance = context.Request.Path
                    });
                }
                catch (PasswordResetValidationException ex)
                {
                    return TypedResults.ValidationProblem(ex.Errors,
                        title: "Reset Password Validation Failed",
                        instance: context.Request.Path);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred. Please try again later.",
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
            .WithName("ResetPassword")
            .WithTags("Authentication")
            .WithSummary("Reset user password")
            .WithDescription("Resets the user’s password using the reset code sent to their email.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        /// login              
        public static RouteGroupBuilder MapLoginEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/login", async Task<Results<
                Ok<LoginResponse>,
                BadRequest<ProblemDetails>,
                UnauthorizedHttpResult,
                ProblemHttpResult,
                ValidationProblem>> (
                [FromBody] LoginRequest request,
                HttpContext context,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    return await authService.Login(request);
                }
                catch (InvalidCredentialsException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid credentials",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }
                catch (TwoFactorRequiredException ex)
                {
                    var response = new LoginResponse
                    {
                        Username = ex.User.UserName,
                        Emailaddress = ex.User.Email,
                        Mobilenumber = ex.User.PhoneNumber,
                        AccessToken = string.Empty,
                        RefreshToken = string.Empty,
                        IsTwoFactorEnabled = true
                    };

                    return TypedResults.Ok(response);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred. Please try again later.",
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
            .WithName("Login")
            .WithTags("Authentication")
            .WithSummary("User login")
            .WithDescription("Logs in a user using email/username/phone and password. If 2FA is enabled, a verification code is sent to the email.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        // Send 2FA OTP (for login or resend)
        public static RouteGroupBuilder MapSend2FAOtpEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/send-2fa", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>> (
                [FromBody] Send2FARequest request,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log
            ) =>
            {
                try
                {
                    return await authService.SendTwoFactorOtp(request);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        title: "Unexpected Error",
                        detail: "An unexpected error occurred during OTP dispatch.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("Send2FAOtp")
            .WithTags("Authentication")
            .WithSummary("Send 2FA OTP")
            .WithDescription("Sends a two-factor authentication OTP using either email or phone.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        // verify twofactorAuth
        public static RouteGroupBuilder MapVerify2FAEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/verify-2fa", async Task<Results<
                Ok<LoginResponse>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>> (
                [FromBody] Verify2FARequest request,
                [FromServices] IAuthService authService,
                [FromServices] IEventlogger log,
                HttpContext context
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
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred. Please try again later.",
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
            .WithName("VerifyTwoFactor")
            .WithTags("Authentication")
            .WithSummary("Verify Two-Factor Authentication")
            .WithDescription("Verifies the OTP code sent to the user's email or phone, and completes login.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        // refresh
        public static RouteGroupBuilder MapRefreshTokenEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/refresh", async Task<Results<Ok<RefreshTokenResponse>,
             BadRequest<ProblemDetails>,
             ProblemHttpResult,
             UnauthorizedHttpResult>> 
             ( [FromBody] RefreshTokenRequest request,
               [FromServices] IAuthService authService,
               [FromServices] IEventlogger log,
               HttpContext context
             ) =>
            {
                try
                {
                    var userId = context.User.GetId();
                    return await authService.RefreshToken(userId, request);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred. Please try again later.",
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
            .WithName("RefreshToken")
            .WithTags("Authentication")
            .WithSummary("Refresh JWT access token")
            .WithDescription("Uses a valid refresh token to generate new access and refresh tokens.")
            .Produces<RefreshTokenResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            return group;
        }

        // twofactorEnabled
        public static RouteGroupBuilder MapTwoFactorAuthEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/manage/2fa", async Task<Results<Ok<string>,
            Accepted<string>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
            (
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
                        detail: "An unexpected error occurred. Please try again later.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("ToggleTwoFactor")
            .WithTags("Manage Account")
            .WithSummary("Enable or disable Two-Factor Authentication")
            .WithDescription("Allows user to enable or disable 2FA via email or phone.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        // get manageinfo
        public static RouteGroupBuilder MapGetProfileEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/manage/info", async Task<IResult>
                ( HttpContext context,
                  [FromServices] IAuthService authService,
                  [FromServices] IEventlogger log ) =>
            {
                try
                {
                    var userId = context.User.GetId(); // Extension method
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
                        detail: "An unexpected error occurred. Please try again later.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
             .WithName("GetProfile")
             .WithTags("Manage Account")
             .WithSummary("Get user profile information")
             .WithDescription("Retrieves profile data using logged-in user's ID.")
             .Produces<object>(StatusCodes.Status200OK)
             .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
             .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
             .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
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
             [FromServices] IEventlogger log ) =>
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
                        detail: "An unexpected error occurred. Please try again later.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("UpdateProfile")
            .WithTags("Manage Account")
            .WithSummary("Update user profile")
            .WithDescription("Allows a user to update their profile details.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

            return group;
        }

        // log out endpoint
        public static RouteGroupBuilder MapLogoutEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/logout", async Task<Results<Ok<string>,
            NotFound<ProblemDetails>,
            ProblemHttpResult>> (
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
                        return TypedResults.Problem(
                            title: "Unauthorized",
                            detail: "User is not authenticated.",
                            statusCode: StatusCodes.Status401Unauthorized);
                    }

                    return await authService.Logout(userId);
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    return TypedResults.Problem(
                        title: "Logout Error",
                        detail: "An unexpected error occurred during logout.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("Logout")
            .WithTags("Authentication")
            .WithSummary("Logs out the user")
            .WithDescription("Removes tokens and signs the user out.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            return group;
        }
}
}
