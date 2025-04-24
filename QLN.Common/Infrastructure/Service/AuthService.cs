using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

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

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IExtendedEmailSender<ApplicationUser> emailSender,
            LinkGenerator linkGenerator,
            ITokenService tokenService,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _linkGenerator = linkGenerator;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Results<Ok<ApiResponse<string>>, ValidationProblem>> RegisterAsync(RegisterRequest request, HttpContext context)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Emailaddress);
            if (existingUser != null)
            {
                var errors = new Dictionary<string, string[]>
                {
                    { "Email", new[] { "Email address is already in use." } }
                };
                return TypedResults.ValidationProblem(errors);
            }

            var user = new ApplicationUser
            {
                UserName = request.Username,
                Firstname = request.FirstName,
                Lastname = request.Lastname,
                Dateofbirth = request.Dateofbirth,
                Gender = request.Gender,
                Mobileoperator = request.MobileOperator,
                PhoneNumber = request.Mobilenumber,
                Email = request.Emailaddress,
                Nationality = request.Nationality,
                Languagepreferences = request.Languagepreferences,
                Location = request.Location,
                IsCompany = false,
                Isactive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
                return TypedResults.ValidationProblem(errors);
            }

            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new IdentityRole<Guid>("User"));

            await _userManager.AddToRoleAsync(user, "User");

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var confirmUrl = _linkGenerator.GetUriByName(context, "ConfirmEmail", new
            {
                userId = user.Id,
                code = encodedCode
            });

            if (confirmUrl != null)
            {
                await _emailSender.SendConfirmationLinkAsync(user, user.Email, HtmlEncoder.Default.Encode(confirmUrl));
            }

            var response = ApiResponse<string>.Success("User registered successfully. Please check your email to confirm your account.", null);
            return TypedResults.Ok(response);
        }

        public async Task<Results<Ok<ApiResponse<string>>, BadRequest<string>, NotFound<string>, ValidationProblem>> ConfirmEmailAsync(Guid userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return TypedResults.NotFound("User not Found");

            if (await _userManager.IsEmailConfirmedAsync(user))
                return TypedResults.BadRequest("Email is already confirmed");

            var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, decodedCode);

            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
                return TypedResults.ValidationProblem(errors);
            }

            return TypedResults.Ok(ApiResponse<string>.Success("Email confirmed successfully."));
        }

        public async Task<Results<Ok<ApiResponse<string>>, NotFound>> ResendEmailAsync(ResendConfirmationEmailRequest request,HttpContext context)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return TypedResults.NotFound();
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var confirmUrl = _linkGenerator.GetUriByName(context, "ConfirmEmail", new
            {
                userId = user.Id,
                code = encodedCode
            });

            if (confirmUrl != null)
            {
                await _emailSender.SendConfirmationLinkAsync(user, user.Email, HtmlEncoder.Default.Encode(confirmUrl));
            }

            return TypedResults.Ok(ApiResponse<string>.Success("Confirmation link resent. Please check your email."));
        }

        public async Task<Ok<ApiResponse<string>>> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                await _emailSender.SendPasswordResetCodeAsync(user, user.Email, encodedCode);
            }
            return TypedResults.Ok(ApiResponse<string>.Success("If your email is registered and confirmed, a reset code has been sent."));
        }

        public async Task<Results<Ok<ApiResponse<string>>, ValidationProblem>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                return TypedResults.Ok(ApiResponse<string>.Success("If your email is registered, you will receive a password reset link"));
            }

            var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.ResetCode));
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

        public async Task<Results<Ok<ApiResponse<LoginResponse>>, UnauthorizedHttpResult, ValidationProblem>> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                u.UserName == request.UsernameOrEmailOrPhone ||
                u.Email == request.UsernameOrEmailOrPhone ||
                u.PhoneNumber == request.UsernameOrEmailOrPhone);

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                return TypedResults.Unauthorized();

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]> {
                { "Email", new[] { "Email not confirmed." } }
            });
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
                user.Email,
                user.PhoneNumber,
                user.Gender,
                user.Dateofbirth,
                user.Location,
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
            user.PhoneNumber = request.MobileNumber;
            user.Location = request.Location;

            await _userManager.UpdateAsync(user);

            return TypedResults.Ok(ApiResponse<string>.Success("Profile updated successfully"));
        }
    }
}
