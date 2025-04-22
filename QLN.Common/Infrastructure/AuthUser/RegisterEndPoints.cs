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


namespace QLN.Common.Infrastructure.AuthUser
{
    public static class RegisterEndPoints
    {
        // register
        public static RouteGroupBuilder MapRegisterEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/register", async Task<Results<Ok<ApiResponse<string>>, ValidationProblem>>
            (
                [FromBody] RegisterRequest request,
                HttpContext context,
                [FromServices] UserManager<ApplicationUser> userManager,
                [FromServices] IEmailSender<ApplicationUser> emailSender,
                [FromServices] LinkGenerator linkGenerator
            ) =>
            {
                var user = new ApplicationUser
                {
                    UserName = request.Username,
                    Username = request.Username,
                    Firstname = request.FirstName,
                    Lastname = request.Lastname,
                    Dateofbirth = request.Dateofbirth,
                    Gender = request.Gender,
                    Mobileoperator = request.MobileOperator,
                    Mobilenumber = request.Mobilenumber,
                    Emailaddress = request.Emailaddress,
                    Email = request.Emailaddress,
                    Nationality = request.Nationality,
                    Languagepreferences = request.Languagepreferences,
                    Location = request.Location,
                    Isactive = true
                };

                var result = await userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    var errors = result.Errors
                        .GroupBy(e => e.Code)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

                    return TypedResults.ValidationProblem(errors);
                }

                var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var confirmUrl = linkGenerator.GetUriByName(context, "ConfirmEmail", new
                {
                    userId = user.Id,
                    code = encodedCode
                });

                if (confirmUrl != null)
                {
                    await emailSender.SendConfirmationLinkAsync(user, user.Email, HtmlEncoder.Default.Encode(confirmUrl));
                }

                var response = ApiResponse<string>.Success("User registered successfully. Please check your email to confirm your account.", null);
                return TypedResults.Ok(response);
            }).WithName("Register")            
            .WithTags("Authentication")
            .WithSummary("Register a new user")
            .WithDescription("Registers a new user and sends an email confirmation link.");

            return group;
        }

        // confirm email
        public static RouteGroupBuilder MapConfirmEmailEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/confirm-email", async Task<Results<Ok<ApiResponse<string>>, NotFound<string>, ValidationProblem>>
            (
                [FromQuery] Guid userId,
                [FromQuery] string code,
                [FromServices] UserManager<ApplicationUser> userManager
            ) =>
            {
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return TypedResults.NotFound("User not Fouund");
                }
                var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                var result = await userManager.ConfirmEmailAsync(user, decodedCode);
                if (!result.Succeeded)
                {
                    var errors = result.Errors
                        .GroupBy(e => e.Code)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
                    return TypedResults.ValidationProblem(errors);
                }
                //return TypedResults.Ok();
                return TypedResults.Ok(ApiResponse<string>.Success("Email confirmed successfully."));
            }).WithName("ConfirmEmail")
            .WithTags("Authentication")
            .WithSummary("Confirm email address")
            .WithDescription("Verifies the email using confirmation link parameters.");
            return group;
        }

        // Resend Confirmation Email
        public static RouteGroupBuilder MapResendConfirmationEmailEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/resend-confirmation-email", async Task<Results<Ok<ApiResponse<string>>, NotFound>> (
                [FromBody] ResendConfirmationEmailRequest request,
                HttpContext context,
                [FromServices] UserManager<ApplicationUser> userManager,
                [FromServices] IEmailSender<ApplicationUser> emailSender,
                [FromServices] LinkGenerator linkGenerator
            ) =>
            {
                var user = await userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {                   
                    return TypedResults.NotFound();
                }

                var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var confirmUrl = linkGenerator.GetUriByName(context, "ConfirmEmail", new
                {
                    userId = user.Id,
                    code = encodedCode
                });

                if (confirmUrl != null)
                {
                    await emailSender.SendConfirmationLinkAsync(user, user.Email, HtmlEncoder.Default.Encode(confirmUrl));
                }

                return TypedResults.Ok(ApiResponse<string>.Success("Confirmation link resent. Please check your email."));
            })
            .WithName("ResendConfirmationEmail")
            .WithTags("Authentication")
            .WithSummary("Resend email confirmation link")
            .WithDescription("Sends a new confirmation link to the user’s email if the original confirmation email was not received or expired.");

            return group;
        }

        // Forgot Password
        public static RouteGroupBuilder MapForgotPasswordEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/forgot-password", async Task<Ok<ApiResponse<string>>>
            (
                [FromBody] ForgotPasswordRequest request,
                [FromServices] UserManager<ApplicationUser> userManager,
                [FromServices] IEmailSender<ApplicationUser> emailSender
            ) =>
            {
                var user = await userManager.FindByEmailAsync(request.Email);
                if (user != null && await userManager.IsEmailConfirmedAsync(user))
                {
                    var code = await userManager.GeneratePasswordResetTokenAsync(user);
                    var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    await emailSender.SendPasswordResetCodeAsync(user, user.Email, encodedCode);
                }

                return TypedResults.Ok(ApiResponse<string>.Success("If your email is registered and confirmed, a reset code has been sent."));
            })
                .WithName("ForgotPassword")
                .WithTags("Authentication")
                .WithSummary("Request password reset")
                .WithDescription("Sends a password reset token to the user’s email if the email is registered and confirmed.");

            return group;
        }

        // Reset Password
        public static RouteGroupBuilder MapResetPasswordEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/reset-password", async Task<Results<Ok<ApiResponse<string>> , ValidationProblem>>
            (
                [FromBody] ResetPasswordRequest request,
                [FromServices] UserManager<ApplicationUser> userManager
            ) =>
            {
                var user = await userManager.FindByEmailAsync(request.Email);
                if (user == null || !await userManager.IsEmailConfirmedAsync(user))
                {
                    return TypedResults.Ok(ApiResponse<string>
                        .Success("If your email is registered, you will receive a password reset link"));
                }

                var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.ResetCode));
                var result = await userManager.ResetPasswordAsync(user, decodedCode, request.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors
                        .GroupBy(e => e.Code)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
                    return TypedResults.ValidationProblem(errors);
                }

                return TypedResults.Ok(ApiResponse<string>.Success("Password has been reset successfully"));
            }).WithName("ResetPassword")
            .WithTags("Authentication")
            .WithSummary("Reset user password")
            .WithDescription("Resets the password using the token provided via email.");
            return group;
        }

        // login              
        public static RouteGroupBuilder MapLoginEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/login", async Task<Results<Ok<ApiResponse<LoginResponse>>, UnauthorizedHttpResult, ValidationProblem>>
            (
                [FromBody] LoginRequest request,
                [FromServices] UserManager<ApplicationUser> userManager,
                [FromServices] SignInManager<ApplicationUser> signInManager,
                [FromServices] ITokenService tokenService,
                [FromServices] IEmailSender<ApplicationUser> emailSender
            ) =>
            {
                ApplicationUser? user = null;

                
                user = await userManager.Users.FirstOrDefaultAsync(u =>
                    u.UserName == request.UsernameOrEmailOrPhone ||
                    u.Email == request.UsernameOrEmailOrPhone ||
                    u.Mobilenumber == request.UsernameOrEmailOrPhone
                );

                if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
                    return TypedResults.Unauthorized();

                if (!await userManager.IsEmailConfirmedAsync(user))
                {
                    return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { 
                            "Email", new[] { "Email not confirmed." } 
                        }  
                    });
                }

                if (user.TwoFactorEnabled)
                {
                    // Generate OTP
                    var code = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

                    // Send via email
                    await emailSender.SendPasswordResetCodeAsync(user, user.Email, code);

                    return TypedResults.Ok(ApiResponse<LoginResponse>.Success("2FA code sent to email. Please verify to complete login.", new LoginResponse
                    {
                        Username = user.Username,
                        Emailaddress = user.Emailaddress,
                        Mobilenumber = user.Mobilenumber,
                        AccessToken = string.Empty, // Don't send token until verified
                        RefreshToken = string.Empty,
                        IsTwoFactorEnabled = user.TwoFactorEnabled
                    }));
                }


                var accessToken = await tokenService.GenerateAccessToken(user);
                var refreshToken = tokenService.GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                await userManager.UpdateAsync(user);

                return TypedResults.Ok(ApiResponse<LoginResponse>.Success("Login successful", new LoginResponse
                {                    
                    Username = user.Username,
                    Emailaddress = user.Emailaddress,
                    Mobilenumber = user.Mobilenumber,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken                    
                }));
            }).WithName("Login")
            .WithTags("Authentication")
            .WithSummary("Logs in a user")
            .WithDescription("Logs in using username/email/phone and password. If 2FA is enabled, sends an OTP.");

            return group;
        }

        // verify twofactorAuth
        public static RouteGroupBuilder MapVerify2FAEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/verify-2fa", async Task<Results<Ok<ApiResponse<LoginResponse>>, ValidationProblem, NotFound>>
            (
                [FromBody] Verify2FARequest request,
                [FromServices] UserManager<ApplicationUser> userManager,
                [FromServices] ITokenService tokenService
            ) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u =>
                    u.UserName == request.UsernameOrEmailOrPhone ||
                    u.Email == request.UsernameOrEmailOrPhone ||
                    u.Mobilenumber == request.UsernameOrEmailOrPhone
                );

                if (user == null)
                    return TypedResults.NotFound();

                if (!user.TwoFactorEnabled)
                {
                    return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                { "2FA", new[] { "Two-Factor Authentication is not enabled for this user." } }
            });
                }

                var is2faTokenValid = await userManager.VerifyTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultEmailProvider,
                    request.TwoFactorCode
                );

                if (!is2faTokenValid)
                {
                    return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { 
                            "2FA", new[] { "Invalid two-factor authentication code." } 
                        }
                    });
                }

                var accessToken = await tokenService.GenerateAccessToken(user);
                var refreshToken = tokenService.GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                await userManager.UpdateAsync(user);

                return TypedResults.Ok(ApiResponse<LoginResponse>.Success("2FA verified. Login successful.", new LoginResponse
                {
                    Username = user.Username,
                    Emailaddress = user.Emailaddress,
                    Mobilenumber = user.Mobilenumber,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                }));
            }).WithName("VerifyTwoFactor")
            .WithTags("Authentication")
            .WithSummary("Verify Two-Factor Authentication")
            .WithDescription("Verifies the OTP code sent to the user's email and completes login by issuing tokens.");

            return group;
        }

        // refresh
        public static RouteGroupBuilder MapRefreshTokenEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/refresh", async Task<Results<Ok<ApiResponse<RefreshTokenResponse>>, UnauthorizedHttpResult>>
            (
                [FromBody] RefreshTokenRequest request,
                [FromServices] UserManager<ApplicationUser> userManager,
                [FromServices] ITokenService tokenService
            ) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u =>
                    u.RefreshToken == request.RefreshToken &&
                    u.RefreshTokenExpiry > DateTime.UtcNow);

                if (user == null)
                    return TypedResults.Unauthorized();

                var newAccessToken = await tokenService.GenerateAccessToken(user);
                var newRefreshToken = tokenService.GenerateRefreshToken();

                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                await userManager.UpdateAsync(user);

                return TypedResults.Ok(ApiResponse<RefreshTokenResponse>.Success("Token refreshed", new RefreshTokenResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,                    
                }));
            }).WithName("RefreshToken")
            .WithTags("Authentication")
            .WithSummary("Refresh JWT access token").WithDescription("Uses a valid refresh token to generate a new access and refresh token.");

            return group;
        }

        // twofactorEnabled
        public static RouteGroupBuilder MapTwoFactorAuthEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/manage/2fa", async Task<IResult> (
                [FromBody] TwoFactorToggleRequest request,                
                [FromServices] UserManager<ApplicationUser> userManager
            ) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u =>
                u.Email == request.EmailorPhoneNumber || u.Mobilenumber == request.EmailorPhoneNumber);
                if (user == null)
                    return TypedResults.Unauthorized();

                user.TwoFactorEnabled = request.Enable;
                await userManager.UpdateAsync(user);

                var status = request.Enable ? "enabled" : "disabled";
                return TypedResults.Ok(ApiResponse<string>.Success($"Two-Factor Authentication has been {status}."));
            })
                .WithName("ToggleTwoFactor")
                .WithTags("Manage Account")
                .WithSummary("Enable or disable two-factor authentication")
                .WithDescription("Allows user to enable or disable 2FA based on email or phone number.");

            return group;
        }

        // get manageinfo
        public static RouteGroupBuilder MapGetProfileEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/manage/info", async Task<IResult> (
                string identitity,
                [FromServices] UserManager<ApplicationUser> userManager
            ) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.Email == identitity);
                if (user == null)
                    return TypedResults.Unauthorized();

                return TypedResults.Ok(ApiResponse<object>.Success("Profile data", new
                {
                    user.Username,
                    user.Emailaddress,
                    user.Mobilenumber,
                    user.Gender,
                    user.Dateofbirth,                    
                    user.Location,
                    user.Isactive,
                    user.TwoFactorEnabled
                }));
            }).WithName("GetProfileInfo")
            .WithTags("Manage Account")
            .WithSummary("Get user profile information")
            .WithDescription("Returns user profile details using their email as identity input.");

            return group;
        }

        // post manageinfo (update profile)
        public static RouteGroupBuilder MapUpdateProfileEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/manage/info", async Task<IResult> (
                [FromBody] UpdateProfileRequest request,                
                [FromServices] UserManager<ApplicationUser> userManager
            ) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u =>
                u.UserName == request.UsernameOrEmailOrPhone ||
                u.Email == request.UsernameOrEmailOrPhone ||
                u.Mobilenumber == request.UsernameOrEmailOrPhone);

                if (user == null)
                    return TypedResults.Unauthorized();

                user.Firstname = request.FirstName;
                user.Lastname = request.LastName;
                user.Mobilenumber = request.MobileNumber;
                user.Location = request.Location;
                await userManager.UpdateAsync(user);

                return TypedResults.Ok(ApiResponse<string>.Success("Profile updated successfully"));
            })
                .WithName("UpdateProfileInfo")
                .WithTags("Manage Account")
                .WithSummary("Update user profile")
                .WithDescription("Allows user to update personal details like name, mobile number, and location.");

            return group;
        }

       

    }
}
