using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.IAuthService;
using QLN.Common.Infrastructure.IService.IEmailService;
using QLN.Common.Infrastructure.IService.ITokenService;
using QLN.Common.Infrastructure.Model;
using System.Text;
using System.Text.RegularExpressions;

namespace QLN.Common.Infrastructure.Service.AuthService
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
        private readonly IWebHostEnvironment _env;


        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IExtendedEmailSender<ApplicationUser> emailSender,
            LinkGenerator linkGenerator,
            ITokenService tokenService,
            IConfiguration configuration,
            IEventlogger logger,
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment env
            )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _linkGenerator = linkGenerator;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
            _config = configuration;
            _log = logger;
            _env = env;
        }


        public async Task<string> Register(RegisterRequest request, HttpContext context)
        {
            var isBypassUser = _env.IsDevelopment() &&
                               request.Emailaddress == ConstantValues.ByPassEmail &&
                               request.Mobilenumber == ConstantValues.ByPassMobile;

            if (isBypassUser)
            {
                var existingBypassUser = await _userManager.Users
                    .FirstOrDefaultAsync(r => r.Email == request.Emailaddress || r.PhoneNumber == request.Mobilenumber);
                if (existingBypassUser != null)
                    await _userManager.DeleteAsync(existingBypassUser);
            }

            if (!isBypassUser &&
                (!TempVerificationStore.VerifiedEmails.Contains(request.Emailaddress) ||
                 !TempVerificationStore.VerifiedPhoneNumbers.Contains(request.Mobilenumber)))
            {
                throw new VerificationRequiredException();
            }

            if (await _userManager.FindByEmailAsync(request.Emailaddress) is not null)
                throw new EmailAlreadyRegisteredException();

            if (await _userManager.FindByNameAsync(request.Username) is not null)
                throw new UsernameTakenException(request.Username);

            var mobileRegex = new Regex(@"^(\+?\d{1,3})?[\s]?\d{10,15}$");
            if (!mobileRegex.IsMatch(request.Mobilenumber))
                throw new InvalidMobileFormatException();

            if (!IsValidEmail(request.Emailaddress))
                throw new InvalidEmailFormatException();

            var newUser = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Emailaddress,
                PhoneNumber = request.Mobilenumber,
                FirstName = request.FirstName,
                LastName = request.Lastname,
                DateOfBirth = request.Dateofbirth,
                MobileOperator = request.MobileOperator,
                Nationality = request.Nationality,
                LanguagePreferences = request.Languagepreferences,
                IsCompany = false,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                TwoFactorEnabled = request.TwoFactorEnabled,
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
            };

            var createResult = await _userManager.CreateAsync(newUser, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
                throw new RegistrationValidationException(errors);
            }

            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new IdentityRole<Guid>("User"));

            await _userManager.AddToRoleAsync(newUser, "User");

            if (!isBypassUser)
            {
                TempVerificationStore.VerifiedEmails.Remove(request.Emailaddress);
                TempVerificationStore.VerifiedPhoneNumbers.Remove(request.Mobilenumber);
            }

            return "User registered successfully.";
        }
        public async Task<Results<Ok<string>, ProblemHttpResult, Conflict<ProblemDetails>>> SendEmailOtp(string email)
        {

            var isBypassUser = _env.IsDevelopment() && email == ConstantValues.ByPassEmail;
            if (isBypassUser)
            {
                return TypedResults.Ok(("OTP bypassed."));

            }

            if (!IsValidEmail(email))
            {
                throw new InvalidEmailFormatException();
            }

            var existingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (existingUser != null)
            {
                throw new EmailAlreadyRegisteredException();
            }

            var otp = new Random().Next(100000, 999999).ToString();
            TempVerificationStore.EmailOtps[email] = otp;

            await _emailSender.SendOtpEmailAsync(email, otp);

            return TypedResults.Ok("OTP sent to your email.");

        }


        public async Task<Results<Ok<string>, ProblemHttpResult, NotFound<string>, BadRequest<string>>> VerifyEmailOtp(string email, string otp)
        {
            try
            {
                var isBypassUser = _env.IsDevelopment() &&
                    email == ConstantValues.ByPassEmail;

                if (isBypassUser)
                {
                    return TypedResults.Ok("Email bypassed.");
                }

                if (!TempVerificationStore.EmailOtps.TryGetValue(email, out var storedOtp))
                {
                    return TypedResults.NotFound("OTP not requested.");
                }

                if (storedOtp != otp)
                {
                    return TypedResults.NotFound("Invalid OTP.");
                }

                TempVerificationStore.VerifiedEmails.Add(email);
                TempVerificationStore.EmailOtps.Remove(email);

                return TypedResults.Ok("Email verified successfully.");
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    detail: "An unexpected error occurred. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        public async Task<Results<Ok<string>, ProblemHttpResult, Conflict<ProblemDetails>>> SendPhoneOtp(string phoneNumber)
        {
            try
            {
                var isBypassUser = _env.IsDevelopment() &&
                                   phoneNumber == ConstantValues.ByPassMobile;

                if (isBypassUser)
                {
                    return TypedResults.Ok("OTP bypassed.");
                }

                var existingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.IsActive);
                if (existingUser != null)
                {
                    throw new PhoneAlreadyRegisteredException("Phone already registered.");
                }

                var otp = new Random().Next(100000, 999999).ToString();
                TempVerificationStore.PhoneOtps[phoneNumber] = otp;

                string smsText = $"Your OTP for verification is {otp}.";
                var customerId = _config["OoredooSmsApi:CustomerId"];
                var userName = _config["OoredooSmsApi:UserName"];
                var userPassword = _config["OoredooSmsApi:UserPassword"];
                var originator = _config["OoredooSmsApi:Originator"];
                var apiUrl = _config["OoredooSmsApi:ApiUrl"];

                var response = await SendSms(apiUrl, customerId, userName, userPassword, phoneNumber, smsText, originator);

                if (!response.IsSuccessStatusCode)
                {
                    throw new SmsSendingFailedException("Failed to send SMS.");
                }

                return TypedResults.Ok("OTP sent successfully to phone.");
            }
            catch (PhoneAlreadyRegisteredException ex)
            {
                return TypedResults.Conflict(new ProblemDetails
                {
                    Title = "Phone Already Registered",
                    Detail = ex.Message,
                    Status = StatusCodes.Status409Conflict
                });
            }
            catch (SmsSendingFailedException ex)
            {
                return TypedResults.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    detail: "An unexpected error occurred. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }


        public async Task<Results<Ok<string>, ProblemHttpResult, BadRequest<ProblemDetails>>> VerifyPhoneOtp(string phoneNumber, string otp)
        {
            try
            {
                var isBypassUser = _env.IsDevelopment() &&
                    phoneNumber == ConstantValues.ByPassMobile;

                if (isBypassUser)
                {
                    return TypedResults.Ok("PhoneNumber bypassed.");
                }

                if (!TempVerificationStore.PhoneOtps.TryGetValue(phoneNumber, out var storedOtp))
                {
                    throw new PhoneOtpMissingException("OTP not requested for this phone number.");
                }

                if (storedOtp != otp)
                {
                    throw new InvalidPhoneOtpException("Invalid OTP.");
                }

                TempVerificationStore.VerifiedPhoneNumbers.Add(phoneNumber);
                TempVerificationStore.PhoneOtps.Remove(phoneNumber);

                return TypedResults.Ok("Phone number verified successfully.");
            }
            catch (PhoneOtpMissingException ex)
            {
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "OTP Missing",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (InvalidPhoneOtpException ex)
            {
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "Invalid OTP",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    detail: "An unexpected error occurred. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>> ForgotPassword(ForgotPasswordRequest request)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
                    throw new ForgotPasswordUserNotFoundException();

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var baseUrl = _config.GetSection("BaseUrl")["resetPassword"];
                var resetUrl = $"{baseUrl}?email={Uri.EscapeDataString(request.Email)}&code={encodedCode}";

                await _emailSender.SendPasswordResetLinkAsync(user, user.Email, resetUrl);

                return TypedResults.Ok("If your email is registered and confirmed, a password reset link has been sent.");
            }
            catch (ForgotPasswordUserNotFoundException ex)
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
                _log.LogException(ex);
                return TypedResults.Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Results<Ok<string>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, ValidationProblem, ProblemHttpResult>> ResetPassword(ResetPasswordRequest request)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(r => r.Email == request.Email && r.IsActive == true);

                if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
                {
                    throw new ResetPasswordUserNotFoundException();
                }

                var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.ResetCode));
                var isValidToken = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "ResetPassword", decodedCode);

                if (!isValidToken)
                {
                    throw new ResetPasswordInvalidTokenException();
                }

                var result = await _userManager.ResetPasswordAsync(user, decodedCode, request.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors
                        .GroupBy(e => e.Code)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

                    throw new PasswordResetValidationException(errors);
                }

                return TypedResults.Ok("Password has been reset successfully");
            }
            catch (ResetPasswordUserNotFoundException ex)
            {
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "User Not Found or Not Confirmed",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (ResetPasswordInvalidTokenException ex)
            {
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "Invalid or Expired Token",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (PasswordResetValidationException ex)
            {
                return TypedResults.ValidationProblem(ex.Errors,
                    title: "Reset Password Validation Failed");
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    detail: "An unexpected error occurred. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
        public async Task<Results<Ok<LoginResponse>,BadRequest<ProblemDetails>,UnauthorizedHttpResult,ProblemHttpResult,ValidationProblem>> Login(LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UsernameOrEmailOrPhone) || string.IsNullOrWhiteSpace(request.Password))
                {
                    var errors = new Dictionary<string, string[]>();
                    if (string.IsNullOrWhiteSpace(request.UsernameOrEmailOrPhone))
                        errors.Add(nameof(request.UsernameOrEmailOrPhone), new[] { "Username, email, or phone is required." });
                    if (string.IsNullOrWhiteSpace(request.Password))
                        errors.Add(nameof(request.Password), new[] { "Password is required." });

                    return TypedResults.ValidationProblem(errors, title: "Login validation failed");
                }

                var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                    (u.UserName == request.UsernameOrEmailOrPhone ||
                     u.Email == request.UsernameOrEmailOrPhone ||
                     u.PhoneNumber == request.UsernameOrEmailOrPhone) &&
                    u.IsActive == true);

                if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Credentials",
                        Detail = "Username or password is incorrect.",
                        Status = StatusCodes.Status400BadRequest
                    });

                if (user.TwoFactorEnabled)
                {
                    return TypedResults.Ok(new LoginResponse
                    {
                        Username = user.UserName,
                        Emailaddress = user.Email,
                        Mobilenumber = user.PhoneNumber,
                        AccessToken = string.Empty,
                        RefreshToken = string.Empty,
                        IsTwoFactorEnabled = true
                    });
                }

                var accessToken = await _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();

                await _userManager.SetAuthenticationTokenAsync(user, Constants.ConstantValues.QLNProvider, Constants.ConstantValues.RefreshToken, refreshToken);
                await _userManager.SetAuthenticationTokenAsync(user, Constants.ConstantValues.QLNProvider, Constants.ConstantValues.RefreshTokenExpiry, DateTime.UtcNow.AddDays(7).ToString("o"));

                return TypedResults.Ok(new LoginResponse
                {
                    Username = user.UserName,
                    Emailaddress = user.Email,
                    Mobilenumber = user.PhoneNumber,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    IsTwoFactorEnabled = false
                });
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }


        public async Task<Results<Ok<LoginResponse>, BadRequest<ProblemDetails>, ProblemHttpResult>> Verify2FA(Verify2FARequest request)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                    (u.UserName == request.UsernameOrEmailOrPhone ||
                     u.Email == request.UsernameOrEmailOrPhone ||
                     u.PhoneNumber == request.UsernameOrEmailOrPhone) &&
                    u.IsActive == true);

                if (user == null)
                    throw new InvalidCredentialsException("Invalid username/email/phone number.");

                if (!user.TwoFactorEnabled)
                    throw new InvalidOperationException("2FA is not enabled for this user.");

                var isBypassUser = _env.IsDevelopment() &&
                    (user.Email == ConstantValues.ByPassEmail || user.PhoneNumber == ConstantValues.ByPassMobile);

                if (request.Method != ConstantValues.Phone && request.Method != ConstantValues.Email)
                {
                    throw new Exception();
                }
                var provider = request.Method.Equals(ConstantValues.Phone, StringComparison.OrdinalIgnoreCase)
                    ? TokenOptions.DefaultPhoneProvider
                    : TokenOptions.DefaultEmailProvider;


                var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, provider, request.TwoFactorCode);

                if (!isValid && !(isBypassUser && request.TwoFactorCode == ConstantValues.ByPass2FA))
                    throw new InvalidOperationException("Invalid or expired 2FA code.");

                var accessToken = await _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();

                await _userManager.SetAuthenticationTokenAsync(user,
                    Constants.ConstantValues.QLNProvider,
                    Constants.ConstantValues.RefreshToken,
                    refreshToken);

                await _userManager.SetAuthenticationTokenAsync(user,
                    Constants.ConstantValues.QLNProvider,
                    Constants.ConstantValues.RefreshTokenExpiry,
                    DateTime.UtcNow.AddDays(7).ToString("o"));

                var response = new LoginResponse
                {
                    Username = user.UserName,
                    Emailaddress = user.Email,
                    Mobilenumber = user.PhoneNumber,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    IsTwoFactorEnabled = true
                };

                return TypedResults.Ok(response);
            }
            catch (InvalidCredentialsException ex)
            {
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Credentials",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "2FA Verification Failed",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Results<Ok<RefreshTokenResponse>, BadRequest<ProblemDetails>, ProblemHttpResult, UnauthorizedHttpResult>> RefreshToken(Guid userId,RefreshTokenRequest request)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive == true);

                if (user == null)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Token",
                        Detail = "Refresh token is not valid.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                var storedToken = await _userManager.GetAuthenticationTokenAsync(
                        user,
                        Constants.ConstantValues.QLNProvider,
                        Constants.ConstantValues.RefreshToken);

                var expiryStr = await _userManager.GetAuthenticationTokenAsync(
                    user,
                    Constants.ConstantValues.QLNProvider,
                    Constants.ConstantValues.RefreshTokenExpiry);

                if (storedToken == request.RefreshToken)
                {
                    if (!DateTime.TryParse(expiryStr, out var expiry))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Token Expiry",
                            Detail = "Refresh token expiry date is invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    if (expiry <= DateTime.UtcNow)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var newAccessToken = await _tokenService.GenerateAccessToken(user);
                    var newRefreshToken = _tokenService.GenerateRefreshToken();

                    await _userManager.SetAuthenticationTokenAsync(user,
                        Constants.ConstantValues.QLNProvider,
                        Constants.ConstantValues.RefreshToken,
                        newRefreshToken);

                    await _userManager.SetAuthenticationTokenAsync(user,
                        Constants.ConstantValues.QLNProvider,
                        Constants.ConstantValues.RefreshTokenExpiry,
                        DateTime.UtcNow.AddDays(7).ToString("o"));

                    return TypedResults.Ok(new RefreshTokenResponse
                    {
                        AccessToken = newAccessToken,
                        RefreshToken = newRefreshToken
                    });
                }
                else
                {
                    return TypedResults.Problem(
                        title: "Refresh Token Invalid",
                        detail: "Refresh Token Invalid. Please try again later.",
                        statusCode: StatusCodes.Status401Unauthorized);

                }

            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Results<Ok<string>, Accepted<string>, BadRequest<ProblemDetails>, ProblemHttpResult>> Toggle2FA(TwoFactorToggleRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.EmailorPhoneNumber))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Missing Input",
                        Detail = "Email or phone number is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                    (u.Email == request.EmailorPhoneNumber || u.PhoneNumber == request.EmailorPhoneNumber) && u.IsActive == true);

                if (user == null)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "User Not Found",
                        Detail = "Invalid email or phone number.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                user.TwoFactorEnabled = request.Enable;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Update Failed",
                        Detail = $"Failed to update 2FA status: {errorMessage}",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var status = request.Enable ? "enabled" : "disabled";
                var message = $"Two-Factor Authentication has been {status}.";

                return request.Enable
                    ? TypedResults.Ok(message)
                    : TypedResults.Accepted(string.Empty, message);
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }
        public async Task<Results<Ok<object>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, ProblemHttpResult>> GetProfile(Guid Id)
        {
            try
            {
                if (Id == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid ID",
                        Detail = "User ID is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == Id && u.IsActive == true);
                if (user == null)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "User Not Found",
                        Detail = "User not found.",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                var profile = new
                {
                    user.UserName,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.PhoneNumber,
                    user.Gender,
                    user.DateOfBirth,
                    user.LanguagePreferences,
                    user.Nationality,
                    user.MobileOperator,
                    user.PhoneNumberConfirmed,
                    user.EmailConfirmed,
                    user.IsCompany,
                    user.TwoFactorEnabled
                };

                return TypedResults.Ok((object)profile); 
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    detail: "An unexpected error occurred. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Results<Ok<string>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, ProblemHttpResult>> UpdateProfile(Guid id, UpdateProfileRequest request)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = "User ID is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive == true);

                if (user == null)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "User Not Found",
                        Detail = "The requested user does not exist or is inactive.",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.Gender = request.Gender;
                user.DateOfBirth = request.Dateofbirth;
                user.Nationality = request.Nationality;
                user.PhoneNumber = request.MobileNumber;
                user.LanguagePreferences = request.Languagepreferences;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Update Failed",
                        Detail = $"Failed to update profile: {errors}",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                return TypedResults.Ok("Profile updated successfully.");
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
        public async Task<Results<Ok<string>, NotFound<ProblemDetails>, ProblemHttpResult>> Logout(Guid id)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive == true);

                if (user == null)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "User Not Found",
                        Detail = "No active user found with the provided ID.",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                await _userManager.RemoveAuthenticationTokenAsync(user, Constants.ConstantValues.QLNProvider, Constants.ConstantValues.RefreshToken);
                await _userManager.RemoveAuthenticationTokenAsync(user, Constants.ConstantValues.QLNProvider, Constants.ConstantValues.RefreshTokenExpiry);

                await _httpContextAccessor.HttpContext!.SignOutAsync();

                return TypedResults.Ok("User logged out successfully.");
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    title: "Logout Error",
                    detail: "An unexpected error occurred during logout.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<HttpResponseMessage> SendSms(string apiUrl, string customerId, string userName, string userPassword, string phoneNumber, string smsText, string originator)
        {
            // Build the query parameters
            string query = $"smsText={Uri.EscapeDataString(smsText)}" +
                           $"&recipientPhone={Uri.EscapeDataString(phoneNumber)}" +
                           "&messageType=Latin" +
                           "&defDate=" +
                           $"&customerID={Uri.EscapeDataString(customerId)}" +
                           $"&userName={Uri.EscapeDataString(userName)}" +
                           $"&userPassword={Uri.EscapeDataString(userPassword)}" +
                           $"&originator={Uri.EscapeDataString(originator)}" +
                           "&blink=false" +
                           "&flash=false" +
                           "&Private=false";

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(apiUrl + "?" + query);
                return response;
            }
        }

        public async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>> SendTwoFactorOtp(Send2FARequest request)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                    (u.UserName == request.UsernameOrEmailOrPhone ||
                     u.Email == request.UsernameOrEmailOrPhone ||
                     u.PhoneNumber == request.UsernameOrEmailOrPhone) &&
                     u.IsActive == true);

                if (user == null)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "User Not Found",
                        Detail = "User does not exist or is inactive.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                if (!user.TwoFactorEnabled)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "2FA Not Enabled",
                        Detail = "Two-Factor Authentication is not enabled for this user.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var method = request.Method?.Trim().ToLowerInvariant();
                var isBypassUser = _env.IsDevelopment() &&
                                   (user.Email == ConstantValues.ByPassEmail || user.PhoneNumber == ConstantValues.ByPassMobile);

                if (isBypassUser)
                {
                    return TypedResults.Ok($"2FA OTP bypassed in development for {method}.");
                }

                if (method == Constants.ConstantValues.Phone.ToLowerInvariant())
                {
                    if (string.IsNullOrWhiteSpace(user.PhoneNumber) || !user.PhoneNumberConfirmed)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Phone Invalid",
                            Detail = "Phone number is not set or confirmed.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var otp = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);
                    var smsText = $"Your 2FA OTP is {otp}";

                    var response = await SendSms(
                        _config["OoredooSmsApi:ApiUrl"],
                        _config["OoredooSmsApi:CustomerId"],
                        _config["OoredooSmsApi:UserName"],
                        _config["OoredooSmsApi:UserPassword"],
                        user.PhoneNumber, smsText,
                        _config["OoredooSmsApi:Originator"]);

                    return response.IsSuccessStatusCode
                        ? TypedResults.Ok("2FA OTP sent via phone.")
                        : TypedResults.Problem(
                            detail: "Failed to send OTP via SMS. Please try again later.",
                            statusCode: StatusCodes.Status500InternalServerError);
                }
                else if (method == Constants.ConstantValues.Email)
                {
                    if (string.IsNullOrWhiteSpace(user.Email) || !user.EmailConfirmed)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Email Invalid",
                            Detail = "Email is not set or confirmed.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
                    await _emailSender.SendTwoFactorCode(user, user.Email, code);

                    return TypedResults.Ok("2FA OTP sent via email.");
                }
                else
                {
                    throw new Exception();
                }
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    title: "Server Error",
                    detail: "An unexpected error occurred. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
        private static bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) &&
                   Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        }

    }
}
