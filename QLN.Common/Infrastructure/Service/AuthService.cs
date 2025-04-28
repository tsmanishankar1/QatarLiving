using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QLN.Common.Infrastructure.Service
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IExtendedEmailSender<ApplicationUser> _emailSender;
        private readonly LinkGenerator _linkGenerator;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _config;
        private readonly IEventlogger _log;


        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IExtendedEmailSender<ApplicationUser> emailSender,
            LinkGenerator linkGenerator,
            ITokenService tokenService,
            IConfiguration configuration,
            IEventlogger logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _linkGenerator = linkGenerator;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
            _config = configuration;
            _log = logger;
        }


        public async Task<IResult> RegisterAsync(RegisterRequest request, HttpContext context)
        {
            try
            {
                // Verification check
                if (!TempVerificationStore.VerifiedEmails.Contains(request.Emailaddress) ||
                    !TempVerificationStore.VerifiedPhoneNumbers.Contains(request.Mobilenumber))
                {
                    return Results.BadRequest(new ApiResponse<string>
                    {
                        Status = false,
                        Message = "Please verify your Email and Phone Number before registering."
                    });
                }

                var existingUser = await _userManager.FindByEmailAsync(request.Emailaddress);
                if (existingUser == null)
                {
                    return Results.NotFound(new ApiResponse<string>
                    {
                        Status = false,
                        Message = "Email not verified. Please verify before registering."
                    });
                }

                if (existingUser.Firstname != "Temp")
                {
                    return Results.Conflict(new ApiResponse<string>
                    {
                        Status = false,
                        Message = "Email is already registered. Please login."
                    });
                }

                // Username check
                var usernameExists = await _userManager.FindByNameAsync(request.Username);
                if (usernameExists != null)
                {
                    return Results.BadRequest(new ApiResponse<string>
                    {
                        Status = false,
                        Message = $"Username '{request.Username}' is already taken."
                    });
                }

                // Mobile validation
                var mobileRegex = new Regex(@"^(\+?\d{1,3})?[\s]?\d{10,15}$");
                if (!mobileRegex.IsMatch(request.Mobilenumber))
                {
                    return Results.BadRequest(new ApiResponse<string>
                    {
                        Status = false,
                        Message = "Invalid mobile number format. Please enter a valid 10 to 15 digits."
                    });
                }

                // Age validation
                var today = DateOnly.FromDateTime(DateTime.Today);
                var age = today.Year - request.Dateofbirth.Year;
                if (request.Dateofbirth > today.AddYears(-age)) age--;

                if (age < 18)
                {
                    return Results.BadRequest(new ApiResponse<string>
                    {
                        Status = false,
                        Message = "You must be at least 18 years old to register."
                    });
                }

                // Update user properties
                existingUser.UserName = request.Username;
                existingUser.Firstname = request.FirstName;
                existingUser.Lastname = request.Lastname;
                existingUser.Dateofbirth = request.Dateofbirth;
                existingUser.Gender = request.Gender;
                existingUser.Mobileoperator = request.MobileOperator;
                existingUser.PhoneNumber = request.Mobilenumber;
                existingUser.Nationality = request.Nationality;
                existingUser.Languagepreferences = request.Languagepreferences;
                existingUser.Location = request.Location;
                existingUser.IsCompany = false;
                existingUser.EmailConfirmed = true;
                existingUser.PhoneNumberConfirmed = true;
                existingUser.Isactive = true;

                var passwordHash = _userManager.PasswordHasher.HashPassword(existingUser, request.Password);
                existingUser.PasswordHash = passwordHash;

                var updateResult = await _userManager.UpdateAsync(existingUser);

                if (!updateResult.Succeeded)
                {
                    var errors = updateResult.Errors
                        .GroupBy(e => e.Code)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

                    return Results.ValidationProblem(errors,
                        detail: "One or more errors occurred during registration.",
                        statusCode: 400);
                }

                if (!await _roleManager.RoleExistsAsync("User"))
                    await _roleManager.CreateAsync(new IdentityRole<Guid>("User"));

                await _userManager.AddToRoleAsync(existingUser, "User");

                return Results.Ok(new ApiResponse<string>
                {
                    Status = true,
                    Message = "User registered successfully."
                });
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return Results.Problem(
                    detail: "An unexpected error occurred. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    extensions: new Dictionary<string, object?>
                    {
                        {
                            "response", new ApiResponse<string>
                            {
                                Status = false,
                                Message = "An unexpected error occurred. Please try again later."
                            }}
                    });
            }
        }

        public async Task<Ok<ApiResponse<string>>> SendEmailOtpAsync(string email)
        {           
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                if (user.Firstname != "Temp")
                {
                    return TypedResults.Ok(ApiResponse<string>.Fail("Email already registered. Please login."));
                }
            }
            else
            {
                // Check if any dummy user with only phone is there
                user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.EmailConfirmed == false && u.Firstname == "Temp");

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = Guid.NewGuid().ToString(),
                        Email = email,
                        PhoneNumber = "0000000000",
                        EmailConfirmed = false,
                        PhoneNumberConfirmed = false,
                        IsCompany = false,
                        Isactive = true,
                        Firstname = "Temp",
                        Lastname = "User",
                        Gender = "Other",
                        Nationality = "Unknown",
                        Dateofbirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)),
                        Languagepreferences = "English",
                        Location = "Unknown",
                        SecurityStamp = Guid.NewGuid().ToString()
                    };

                    var password = Guid.NewGuid().ToString("N") + "@1Aa";
                    await _userManager.CreateAsync(user, password);
                }
                else
                {
                    // Update dummy user with correct email
                    user.Email = email;
                    await _userManager.UpdateAsync(user);
                }
            }

            var otp = await _userManager.GenerateUserTokenAsync(user, "EmailVerification", "EmailOTP");

            await _emailSender.SendOtpEmailAsync(email, otp);

            return TypedResults.Ok(ApiResponse<string>.Success("OTP sent to your email."));
        }
        
        public async Task<Results<Ok<ApiResponse<string>>, BadRequest<string>>> VerifyEmailOtpAsync(string email, string otp)
        {           
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return TypedResults.BadRequest("Invalid email.");

            var isValid = await _userManager.VerifyUserTokenAsync(user, "EmailVerification", "EmailOTP", otp);
            if (!isValid)
                return TypedResults.BadRequest("Invalid or expired OTP.");

            TempVerificationStore.VerifiedEmails.Add(email);

            return TypedResults.Ok(ApiResponse<string>.Success("Email verified successfully."));
        }
        
        public async Task<Ok<ApiResponse<string>>> SendPhoneOtpAsync(string phoneNumber)
        {         
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            if (user != null)
            {
                if (user.Firstname != "Temp")
                {
                    return TypedResults.Ok(ApiResponse<string>.Fail("Phone number already registered. Please login."));
                }
            }
            else
            {
                // Check if any dummy user with only email is there
                user = await _userManager.Users
           .FirstOrDefaultAsync(u => u.PhoneNumber == "0000000000" && u.Firstname == "Temp");

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = Guid.NewGuid().ToString(),
                        Email = $"{Guid.NewGuid()}@temp.com",
                        PhoneNumber = phoneNumber,
                        EmailConfirmed = false,
                        PhoneNumberConfirmed = false,
                        IsCompany = false,
                        Isactive = true,
                        Firstname = "Temp",
                        Lastname = "User",
                        Gender = "Other",
                        Nationality = "Unknown",
                        Dateofbirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)),
                        Languagepreferences = "English",
                        Location = "Unknown",
                        SecurityStamp = Guid.NewGuid().ToString()
                    };

                    var password = Guid.NewGuid().ToString("N") + "@1Aa";
                    await _userManager.CreateAsync(user, password);
                }
                else
                {
                    // Update dummy user with correct phone number
                    user.PhoneNumber = phoneNumber;
                    await _userManager.UpdateAsync(user);
                }
            }

            var otp = await _userManager.GenerateUserTokenAsync(user, "PhoneVerification", "PhoneOTP");

            var path = Path.Combine(Directory.GetCurrentDirectory(), "OtpLogs");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            await File.AppendAllTextAsync(Path.Combine(path, "phone_otps.txt"), $"{phoneNumber}|{otp}|{DateTime.UtcNow}\n");

            return TypedResults.Ok(ApiResponse<string>.Success("OTP generated for phone."));

        }
        
        public async Task<Results<Ok<ApiResponse<string>>, BadRequest<string>>> VerifyPhoneOtpAsync(string phoneNumber, string otp)
        {
           
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (user == null)
                return TypedResults.BadRequest("Invalid phone number.");

            var isValid = await _userManager.VerifyUserTokenAsync(user, "PhoneVerification", "PhoneOTP", otp);
            if (!isValid)
                return TypedResults.BadRequest("Invalid or expired OTP.");

            TempVerificationStore.VerifiedPhoneNumbers.Add(phoneNumber);
            return TypedResults.Ok(ApiResponse<string>.Success("Phone number verified successfully."));
        }

        public async Task<Ok<ApiResponse<string>>> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var baseUrl = _config.GetSection("BaseUrl")["resetPassword"];

                var resetUrl = $"{baseUrl}?email={Uri.EscapeDataString(request.Email)}&code={encodedCode}";

                await _emailSender.SendPasswordResetLinkAsync(user, user.Email, resetUrl);
            }

            return TypedResults.Ok(ApiResponse<string>.Success("If your email is registered and confirmed, a password reset link has been sent."));
        }

        public async Task<Results<Ok<ApiResponse<string>>, ValidationProblem>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                return TypedResults.Ok(ApiResponse<string>.Success("If your email is registered, you will receive a password reset link"));
            }

            var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.ResetCode));

            var isValidToken = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "ResetPassword", decodedCode);
            if (!isValidToken)
            {
                return TypedResults.Ok(ApiResponse<string>.Fail("Invalid or expired token."));
            }
            
            var result = await _userManager.ResetPasswordAsync(user, decodedCode, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
                return TypedResults.ValidationProblem(errors);
            }

            return TypedResults.Ok(ApiResponse<string>.Success("Password has been reset successfully"));
        }

        public async Task<Results<Ok<ApiResponse<LoginResponse>>, BadRequest<ApiResponse<string>>, UnauthorizedHttpResult, ValidationProblem>> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                u.UserName == request.UsernameOrEmailOrPhone ||
                u.Email == request.UsernameOrEmailOrPhone ||
                u.PhoneNumber == request.UsernameOrEmailOrPhone);

                if (user == null)
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Invalid username"));
                }

                if (!await _userManager.CheckPasswordAsync(user, request.Password))
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Invalid password"));
                }

                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Email not confirmed."));
                }


                if (user.TwoFactorEnabled)
                {
                    var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
                    await _emailSender.SendTwoFactorCode(user, user.Email, code);

                    return TypedResults.Ok(ApiResponse<LoginResponse>.Success("2FA code sent to email. Please verify to complete login.", new LoginResponse
                    {
                        Username = user.UserName,
                        Emailaddress = user.Email,
                        Mobilenumber = user.PhoneNumber,
                        AccessToken = string.Empty,
                        RefreshToken = string.Empty,
                        IsTwoFactorEnabled = true
                    }));
                }

                var accessToken = await _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();

                await _userManager.SetAuthenticationTokenAsync(user, QLNTokenConstants.QLNProvider, QLNTokenConstants.RefreshToken, refreshToken);
                await _userManager.SetAuthenticationTokenAsync(user, QLNTokenConstants.QLNProvider, QLNTokenConstants.RefreshTokenExpiry, DateTime.UtcNow.AddDays(7).ToString("o"));

                return TypedResults.Ok(ApiResponse<LoginResponse>.Success("Login successful", new LoginResponse
                {
                    Username = user.UserName,
                    Emailaddress = user.Email,
                    Mobilenumber = user.PhoneNumber,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                }));
            }
            catch(Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.BadRequest(ApiResponse<string>.Fail("An unexpected error occurred. Please try again later."));
            }
        }

        public async Task<Results<Ok<ApiResponse<LoginResponse>>, ValidationProblem, NotFound>> Verify2FAAsync(Verify2FARequest request)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                u.UserName == request.UsernameOrEmailOrPhone ||
                u.Email == request.UsernameOrEmailOrPhone ||
                u.PhoneNumber == request.UsernameOrEmailOrPhone);

            if (user == null)
                return TypedResults.NotFound();

            if (!user.TwoFactorEnabled)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                { "2FA", new[] { "Two-Factor Authentication is not enabled for this user." } }
            });
            }

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, request.TwoFactorCode);
            if (!isValid)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                { "2FA", new[] { "Invalid two-factor authentication code." } }
            });
            }

            var accessToken = await _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            await _userManager.SetAuthenticationTokenAsync(user, QLNTokenConstants.QLNProvider, QLNTokenConstants.RefreshToken, refreshToken);
            await _userManager.SetAuthenticationTokenAsync(user, QLNTokenConstants.QLNProvider, QLNTokenConstants.RefreshTokenExpiry, DateTime.UtcNow.AddDays(7).ToString("o"));

            return TypedResults.Ok(ApiResponse<LoginResponse>.Success("2FA verified. Login successful.", new LoginResponse
            {
                Username = user.UserName,
                Emailaddress = user.Email,
                Mobilenumber = user.PhoneNumber,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            }));
        }

        public async Task<Results<Ok<ApiResponse<RefreshTokenResponse>>, UnauthorizedHttpResult>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            ApplicationUser? user = null;
            foreach (var u in _userManager.Users)
            {
                var storedToken = await _userManager.GetAuthenticationTokenAsync(u, QLNTokenConstants.RefreshToken, "refresh_token");
                var expiryStr = await _userManager.GetAuthenticationTokenAsync(u, QLNTokenConstants.RefreshTokenExpiry, "refresh_token_expiry");

                if (storedToken == request.RefreshToken &&
                    DateTime.TryParse(expiryStr, out var expiry) && expiry > DateTime.UtcNow)
                {
                    user = u;
                    break;
                }
            }

            if (user == null)
                return TypedResults.Unauthorized();

            var newAccessToken = await _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            await _userManager.SetAuthenticationTokenAsync(user, QLNTokenConstants.QLNProvider, QLNTokenConstants.RefreshToken, newRefreshToken);
            await _userManager.SetAuthenticationTokenAsync(user, QLNTokenConstants.QLNProvider, QLNTokenConstants.RefreshTokenExpiry, DateTime.UtcNow.AddDays(7).ToString("o"));

            return TypedResults.Ok(ApiResponse<RefreshTokenResponse>.Success("Token refreshed", new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            }));
        }

        public async Task<IResult> Toggle2FAAsync(TwoFactorToggleRequest request)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                u.Email == request.EmailorPhoneNumber || u.PhoneNumber == request.EmailorPhoneNumber);

            if (user == null)
                return TypedResults.Unauthorized();

            user.TwoFactorEnabled = request.Enable;
            await _userManager.UpdateAsync(user);

            var status = request.Enable ? "enabled" : "disabled";
            return TypedResults.Ok(ApiResponse<string>.Success($"Two-Factor Authentication has been {status}."));
        }

        public async Task<IResult> GetProfileAsync(string identity)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == identity);
            if (user == null)
                return TypedResults.Unauthorized();

            return TypedResults.Ok(ApiResponse<object>.Success("Profile data", new
            {
                user.UserName,
                user.Firstname,
                user.Lastname,
                user.Email,
                user.PhoneNumber,
                user.Gender,
                user.Dateofbirth,
                user.Location,
                user.Languagepreferences,
                user.Nationality,
                user.Mobileoperator,                 
                user.PhoneNumberConfirmed, 
                user.EmailConfirmed,
                user.IsCompany,
                user.Isactive,
                user.TwoFactorEnabled
            }));
        }

        public async Task<IResult> UpdateProfileAsync(UpdateProfileRequest request)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                u.UserName == request.UsernameOrEmailOrPhone ||
                u.Email == request.UsernameOrEmailOrPhone ||
                u.PhoneNumber == request.UsernameOrEmailOrPhone);

            if (user == null)
                return TypedResults.Unauthorized();

            user.Firstname = request.FirstName;
            user.Lastname = request.LastName;                                   
            user.Gender = request.Gender;
            user.Dateofbirth = request.Dateofbirth;
            user.Nationality = request.Nationality;
            user.Location = request.Location;
            user.PhoneNumber = request.MobileNumber;
            user.Languagepreferences = request.Languagepreferences;
            await _userManager.UpdateAsync(user);

            return TypedResults.Ok(ApiResponse<string>.Success("Profile updated successfully"));
        }       

    }
}
