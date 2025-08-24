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
using Microsoft.IdentityModel.Tokens;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Company;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.IAuthService;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Common.Infrastructure.IService.IEmailService;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.IService.ITokenService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Subscriptions;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
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
        private readonly IExternalSubscriptionService _subscriptionService;
        private readonly IV2SubscriptionService _v2SubscriptionService;
        private readonly ICompanyProfileService _companyProfile;
        private readonly HttpClient _httpClient;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IExtendedEmailSender<ApplicationUser> emailSender,
            LinkGenerator linkGenerator,
            ITokenService tokenService,
            IConfiguration configuration,
            IEventlogger logger,
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment env,
            IExternalSubscriptionService subscriptionService,
            IV2SubscriptionService v2SubscriptionService,
            ICompanyProfileService companyProfile,
            IHttpClientFactory httpClientFactory
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
            _subscriptionService = subscriptionService;
            _v2SubscriptionService = v2SubscriptionService;
            _companyProfile = companyProfile;
            _httpClient = httpClientFactory.CreateClient();
        }


        public async Task<string> Register(RegisterRequest request)
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
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                TwoFactorEnabled = request.TwoFactorEnabled,
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

            var existingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == email);
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

                var existingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
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
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

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
                var user = await _userManager.Users.FirstOrDefaultAsync(r => r.Email == request.Email);

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

                var usernameOrEmailOrPhone = request.UsernameOrEmailOrPhone;

                /* var user = await _userManager.FindByNameAsync(usernameOrEmailOrPhone)
                     ?? await _userManager.FindByEmailAsync(usernameOrEmailOrPhone)
                     ?? await _userManager.Users.FirstOrDefaultAsync(u =>
                     u.PhoneNumber == usernameOrEmailOrPhone);*/
                var user = await _userManager.Users
            .Include(u => u.Subscriptions) 
            .Include(x => x.Companies)
            .FirstOrDefaultAsync(u =>
                u.UserName == usernameOrEmailOrPhone ||
                u.Email == usernameOrEmailOrPhone ||
                u.PhoneNumber == usernameOrEmailOrPhone);


                var isValid = await _userManager.CheckPasswordAsync(user, request.Password);
                if (!isValid)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Credentials",
                        Detail = "Username or password is incorrect.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

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
                var drupaluser = new DrupalUser();
                var accessToken = await _tokenService.GenerateEnrichedAccessToken(user, drupaluser, DateTime.UtcNow.AddDays(30),null);
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
                    u.UserName == request.UsernameOrEmailOrPhone ||
                     u.Email == request.UsernameOrEmailOrPhone ||
                     u.PhoneNumber == request.UsernameOrEmailOrPhone);

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

        public async Task<Results<Ok<RefreshTokenResponse>, BadRequest<ProblemDetails>, ProblemHttpResult, UnauthorizedHttpResult>> RefreshToken(Guid userId,DrupalUser drupalUser, string refreshToken)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
                // requires: using Microsoft.Extensions.Configuration;
                var accessTs = _config.GetValue<TimeSpan>("TokenLifetimes:AccessToken");
                var refreshTs = _config.GetValue<TimeSpan>("TokenLifetimes:RefreshToken");

                var accessExpiry = DateTime.UtcNow.Add(accessTs);
                var refreshExpiry = DateTime.UtcNow.Add(refreshTs);
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

                if (storedToken == refreshToken)
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
                    var assignedRoles = await _userManager.GetRolesAsync(user);
                    var newAccessToken = await _tokenService.GenerateEnrichedAccessToken(user, drupalUser, accessExpiry, assignedRoles);
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
                    u.Email == request.EmailorPhoneNumber || u.PhoneNumber == request.EmailorPhoneNumber);

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

                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == Id);
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

                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);

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
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);

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
                    u.UserName == request.UsernameOrEmailOrPhone ||
                     u.Email == request.UsernameOrEmailOrPhone ||
                     u.PhoneNumber == request.UsernameOrEmailOrPhone);

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

        public async Task<Results<Ok<LoginResponse>, BadRequest<ProblemDetails>, UnauthorizedHttpResult, ProblemHttpResult, ValidationProblem>> UserSync(DrupalUser drupalUser, DateTime expiry)
        {
            try
            {
                if (!long.TryParse(drupalUser.Uid, out var userId))
                {
                    Dictionary<string, string[]> errors = new Dictionary<string, string[]>
                    {
                        { nameof(drupalUser.Uid), new[] { "Invalid or missing Drupal UID. Must be a valid integer value." } }
                    };

                    return TypedResults.ValidationProblem(errors, title: "Parsing Drupal ID Error");
                }

                var user = await _userManager.Users
                    .Include(u => u.Subscriptions)   
                    .Include(x => x.Companies)       
                    .FirstOrDefaultAsync(u =>
                        u.LegacyUid == userId);             
                if (user == null)
                {
                    // Create new user with mapped values from DrupalUser
                    var randomPassword = GenerateRandomPassword();
                    
                    user = new ApplicationUser
                    {
                        UserName = drupalUser.Alias,
                        Email = drupalUser.Email,
                        PhoneNumber = drupalUser.Phone ?? null,
                        FirstName = drupalUser.Name,
                        LastName = null,
                        LegacyUid = userId,
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = string.IsNullOrEmpty(drupalUser.Phone) ? true : false,
                        TwoFactorEnabled = false,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        CreatedAt = long.TryParse(drupalUser.Created, out var createdAt) ? EpochTime.DateTime(createdAt) : DateTime.UtcNow,
                        // Map any available fields from DrupalUser if they exist
                        LanguagePreferences = !string.IsNullOrEmpty(drupalUser.Language) ? drupalUser.Language : "en", // Default language,
                        LegacyData = new UserLegacyData()
                        {
                            Access = drupalUser.Access ?? string.Empty,
                            Created = drupalUser.Created ?? string.Empty,
                            Init = drupalUser.Init ?? string.Empty,
                            Status = drupalUser.Status ?? string.Empty,
                            Uid = userId,
                            Alias = drupalUser.Alias,
                            Email = drupalUser.Email,
                            IsAdmin = drupalUser.IsAdmin ?? false,
                            Language = drupalUser.Language,
                            Name = drupalUser.Name,
                            Path = drupalUser.Path,
                            Image = drupalUser.Image,
                            Permissions = drupalUser.Permissions,
                            Phone = drupalUser.Phone,
                            QlnextUserId = drupalUser.QlnextUserId,
                            Roles = drupalUser.Roles,
                        },
                        //LegacySubscription = drupalUser.Subscription != null ? new LegacySubscription
                        //{
                        //    Uid = userId,
                        //    ReferenceId = drupalUser.Subscription.ReferenceId ?? string.Empty,
                        //    StartDate = drupalUser.Subscription.StartDate ?? string.Empty,
                        //    ExpireDate = drupalUser.Subscription.ExpireDate ?? string.Empty,
                        //    ProductType = drupalUser.Subscription.ProductType ?? string.Empty,
                        //    AccessDashboard = drupalUser.Subscription.AccessDashboard,
                        //    ProductClass = drupalUser.Subscription.ProductClass ?? string.Empty,
                        //    Categories = drupalUser.Subscription.Categories,
                        //    SubscriptionCategories = drupalUser.Subscription.SubscriptionCategories,
                        //    Snid = drupalUser.Subscription.Snid ?? string.Empty,
                        //    Status = drupalUser.Subscription.Status ?? string.Empty
                        //} : null
                    };

                    var createResult = await _userManager.CreateAsync(user, randomPassword);
                    if (!createResult.Succeeded)
                    {
                        var errors = createResult.Errors
                            .GroupBy(e => e.Code)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
                        throw new RegistrationValidationException(errors);
                    }

                    bool isCompany = false;
                    //bool hasSubscription = false;

                    var roles = new List<string>
                    {
                        "BasicUser",
                    };

                    if(drupalUser.Roles != null)
                    {
                        // Check if this user has a subscription
                        isCompany = drupalUser.Roles.Contains("business_account");
                        //if(isCompany)
                        //{
                        //    hasSubscription = drupalUser.Roles.Contains("subscription");
                        //    if (hasSubscription)
                        //    {
                        //        if(drupalUser.Subscription != null && long.TryParse(drupalUser.Subscription.ExpireDate, out var subsExpiry))
                        //        {
                        //            var expiryDate = EpochTime.DateTime(subsExpiry);
                        //            if (expiryDate < DateTime.UtcNow)
                        //            {
                        //                hasSubscription = false;
                        //            }
                        //        }
                        //    }
                        //}
                        roles.AddRange(drupalUser.Roles);
                    }

                    foreach(var role in roles)
                    {
                        if (!await _roleManager.RoleExistsAsync(role))
                            await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
                    }

                    await _userManager.AddToRolesAsync(user, roles);

                    if (isCompany)
                    {
                        var company = new CompanyProfile
                        {
                            CompanyName = drupalUser.Name,
                            Email = drupalUser.Email,
                            CompanyType = CompanyType.SME,
                            BusinessDescription = "Migrated Company - please upate your description",
                            PhoneNumber = drupalUser.Phone,
                            WhatsAppNumber = drupalUser.Phone,
                            PhoneNumberCountryCode = "+974",
                            WhatsAppCountryCode = "+974",
                            Status = VerifiedStatus.NeedChanges,
                            Vertical = VerticalType.Classifieds,
                        };
                        // go off and create a company then save the company GUID to the user
                        var companyGuid = Guid.NewGuid();
                        try
                        {
                            // NOTE not working
                            //await _companyProfile.MigrateCompany(companyGuid.ToString(), user.Id.ToString(), user.UserName, company);

                            //user.Companies = new List<UserCompany>
                            //{
                            //    new UserCompany
                            //    {
                            //        DisplayName = drupalUser.Name,
                            //        Id = companyGuid
                            //    }
                            //};

                            //var updateResult = await _userManager.UpdateAsync(user);

                            //if (!updateResult.Succeeded)
                            //{
                            //    var errors = updateResult.Errors
                            //        .GroupBy(e => e.Code)
                            //        .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
                            //    throw new RegistrationValidationException(errors);
                            //}

                            _log.LogTrace($"Company created for userId {user.Id} with companyGuid {companyGuid}");

                        }
                        catch (Exception)
                        {
                            _log.LogError($"Issue creating a company for userId {user.Id}");
                            throw;
                        }
                        
                    }

                    //if(hasSubscription)
                    //{
                    //    // go off and create a subscription with the same information in it then save the subscription GUID to the user
                    //    var environment = _config["LegacySubscriptions:Environment"];
                    //    var type = drupalUser.Subscription?.ProductClass;
                    //    var uid = drupalUser.Subscription?.Uid;

                    //    if(environment != null && type != null && uid != null)
                    //    {

                    //        var subscriptioninfo = type switch
                    //        {
                    //            "item" => await GetLegacySubscription<LegacyItemSubscriptionDrupal>(type, uid, environment), // if Item then fetch Item Subscription
                    //            "service" => await GetLegacySubscription<LegacyItemSubscriptionDrupal>(type, uid, environment), // if Service then fetch Service Subscription
                    //            _ => await GetLegacySubscription<LegacyItemSubscriptionDrupal>(type, uid, environment)
                    //        };

                    //        if(subscriptioninfo != null && subscriptioninfo.Drupal.Item.Status == "success")
                    //        {
                    //            DateTime.TryParse(subscriptioninfo.Drupal.Item.StartDate, out var startDate);
                    //            DateTime.TryParse(subscriptioninfo.Drupal.Item.EndDate, out var endDate);
                    //            TimeSpan duration = endDate - startDate;
                    //            if (duration.TotalDays < 0)
                    //            {
                    //                duration = TimeSpan.FromDays(30); // Default to 30 days if the duration is negative
                    //            }

                    //            var migratedSubscription = new SubscriptionRequestDto
                    //            {
                    //                adsbudget = subscriptioninfo.Drupal.Item.AdsLimitDaily,
                    //                CategoryId = Subscriptions.SubscriptionCategory.Items,
                    //                Currency = "QAR",
                    //                Description = subscriptioninfo.Drupal.Item.Product,
                    //                Duration = duration,
                    //                featurebudget = 0,
                    //                Price = 0,
                    //                promotebudget = 0,
                    //                refreshbudget = int.TryParse(subscriptioninfo.Drupal.Item.RefreshLimitDaily, out var refreshLimitDaily) ? refreshLimitDaily : 0,
                    //                StatusId = Subscriptions.Status.Active,
                    //                SubscriptionName = subscriptioninfo.Drupal.Item.Product,
                    //                VerticalTypeId = subscriptioninfo.Drupal.Item.ProductClass == "item" ? Subscriptions.Vertical.Classifieds : Subscriptions.Vertical.Services // who knows if this is correct ?
                    //            };

                    //            // this will work but wont have any idea what the subscription ID is
                    //            await _subscriptionService.CreateSubscriptionAsync(migratedSubscription);

                    //            //user.Subscriptions = new List<UserSubscription>
                    //            //{
                    //            //    new UserSubscription
                    //            //    {
                    //            //        DisplayName = subscriptioninfo.Drupal.Item.Product,
                    //            //        Id = subscriptionGuid
                    //            //    }
                    //            //};

                    //            //var updateSubResult = await _userManager.UpdateAsync(user);

                    //            //if (!updateSubResult.Succeeded)
                    //            //{
                    //            //    var errors = updateSubResult.Errors
                    //            //        .GroupBy(e => e.Code)
                    //            //        .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
                    //            //    throw new RegistrationValidationException(errors);
                    //            //}

                    //            var subscriptionRole = "Subscriber";

                    //            if (!await _roleManager.RoleExistsAsync(subscriptionRole))
                    //                await _roleManager.CreateAsync(new IdentityRole<Guid>(subscriptionRole));

                    //            // add the user to the Subscriber role
                    //            await _userManager.AddToRoleAsync(user, subscriptionRole);
                    //        }

                    //    }

                    //}
                }
                var activeSubscriptions = new List<V2SubscriptionResponseDto>();
                try
                {
                    var allActiveSubscriptions = await _v2SubscriptionService.GetAllActiveSubscriptionsAsync(user.Id.ToString());
                    activeSubscriptions = allActiveSubscriptions
                        .Where(s => s.ProductType == ProductType.SUBSCRIPTION && s.IsActive)
                        .ToList();
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error fetching active subscriptions for user {UserId}", user.Id);
                }

                var subscriptionsByVertical = activeSubscriptions
                    .GroupBy(s => s.Vertical)
                    .ToDictionary(g => g.Key, g => g.ToList());
                var accessTs = _config.GetValue<TimeSpan>("TokenLifetimes:AccessToken");
                var refreshTs = _config.GetValue<TimeSpan>("TokenLifetimes:RefreshToken");

                var accessExpiry = DateTime.UtcNow.Add(accessTs);
                var refreshExpiry = DateTime.UtcNow.Add(refreshTs);
                var assignedRoles = await _userManager.GetRolesAsync(user);

                // Generate tokens for existing or newly created user
                var accessToken = await _tokenService.GenerateEnrichedAccessToken(user, drupalUser, accessExpiry, assignedRoles);

                // RefreshToken not required yet

                var refreshToken = _tokenService.GenerateRefreshToken();

                await _userManager.SetAuthenticationTokenAsync(user, Constants.ConstantValues.QLNProvider, Constants.ConstantValues.RefreshToken, refreshToken);
                await _userManager.SetAuthenticationTokenAsync(user, Constants.ConstantValues.QLNProvider, Constants.ConstantValues.RefreshTokenExpiry, refreshExpiry.ToString());


                return TypedResults.Ok(new LoginResponse
                {
                    Username = user.UserName,
                    Emailaddress = user.Email,
                    Mobilenumber = user.PhoneNumber,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    IsTwoFactorEnabled = false,
                    ActiveSubscriptions = subscriptionsByVertical
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

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task<LegacyItemSubscriptionDto<T>?> GetLegacySubscription<T>(string type, string uid, string environment, CancellationToken cancellationToken = default) where T : ILegacySubscriptionDrupal
        {

            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("env", environment),
                new KeyValuePair<string, string>("uid", uid),
                new KeyValuePair<string, string>("type", type)
            };
            var content = new FormUrlEncodedContent(formData);

            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            _httpClient.DefaultRequestHeaders.Add("x-api-key", _config["LegacySubscriptions:ApiKey"]);

            var response = await _httpClient.PostAsync(ConstantValues.Subscriptions.SubscriptionsEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                _log.LogError($"Failed to migrate categories. Status: {response.StatusCode}");
                return null;
            }

            _log.LogTrace($"Got Response from migration endpoint {ConstantValues.Subscriptions.SubscriptionsEndpoint}");

            var json = await response.Content.ReadAsStringAsync();

            try
            {
                var jsonDeserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (jsonDeserialized == null)
                {
                    return null;
                }

                var levelDown = JsonSerializer.Serialize(jsonDeserialized.GetValueOrDefault(key: uid)); // serialize it to a string

                var subscription = JsonSerializer.Deserialize<LegacyItemSubscriptionDto<T>>(levelDown, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }); // deserialize into the object we want

                return subscription;
            }
            catch (Exception ex)
            {
                _log.LogError($"Deserialization error: {ex.Message}");
                return null;
            }
        }
        public async Task<Results<Ok<UseMeResponseDto>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, UnauthorizedHttpResult, ProblemHttpResult>> UseMe(Guid userId)
        {
            try
            {
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid User ID",
                        Detail = "User ID is required and cannot be empty.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                // Get user with related data
                var user = await _userManager.Users
                    .Include(u => u.Companies)
                    .Include(u => u.Subscriptions)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "User Not Found",
                        Detail = "User not found or inactive.",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                // Get user roles
                var userRoles = await _userManager.GetRolesAsync(user);

                // Use your existing subscription service methods
                var activeSubscriptions = await _v2SubscriptionService.GetAllActiveSubscriptionsAsync(user.Id.ToString());
                var activeFreeSubscriptions = await _v2SubscriptionService.GetUserFreeSubscriptionsAsync(user.Id.ToString());
                var activeAddons = await _v2SubscriptionService.GetUserAddonsAsync(user.Id.ToString());

                // Group subscriptions by vertical
                var subscriptionsByVertical = activeSubscriptions
                    .Where(s => s.ProductType == ProductType.SUBSCRIPTION)
                    .GroupBy(s => s.Vertical)
                    .ToDictionary(g => g.Key, g => g.ToList());
                var activeP2PSubscriptions = activeSubscriptions
                            .Where(s => s.ProductType == ProductType.PUBLISH)
                            .GroupBy(s => s.Vertical)
                            .ToDictionary(g => g.Key, g => g.ToList());

                var freeSubscriptionsByVertical = activeFreeSubscriptions
                    .GroupBy(s => s.Vertical)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var addonsByVertical = activeAddons
                    .Where(a => a.IsActive)
                    .GroupBy(a => a.Vertical)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Calculate usage summary
                var usageSummary = CalculateOverallUsageSummary(activeSubscriptions, activeFreeSubscriptions, activeAddons);

                // Get FREE ads category usage for all FREE subscriptions using your service
                var freeAdsCategorySummary = new List<CategoryUsageSummaryDto>();
                foreach (var freeSubscription in activeFreeSubscriptions)
                {
                    try
                    {
                        var categorySummary = await _v2SubscriptionService.GetFreeAdsUsageSummaryAsync(freeSubscription.Id);
                        foreach (var category in categorySummary)
                        {
                            freeAdsCategorySummary.Add(new CategoryUsageSummaryDto
                            {
                                Category = ExtractCategoryFromPath(category.CategoryPath),
                                L1Category = ExtractL1CategoryFromPath(category.CategoryPath),
                                L2Category = ExtractL2CategoryFromPath(category.CategoryPath),
                                CategoryPath = category.CategoryPath,
                                AdsAllowed = category.FreeAdsAllowed,
                                AdsUsed = category.FreeAdsUsed,
                                AdsRemaining = category.FreeAdsRemaining,
                                UsagePercentage = category.UsagePercentage
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Error getting FREE ads summary for subscription {Id}", freeSubscription.Id);
                    }
                }

                usageSummary.FreeAdsCategories = freeAdsCategorySummary;

                // Build response
                var response = new UseMeResponseDto
                {
                    User = new UserDetailsDto
                    {
                        Id = user.Id,
                        Username = user.UserName ?? string.Empty,
                        FirstName = user.FirstName ?? string.Empty,
                        LastName = user.LastName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        PhoneNumber = user.PhoneNumber,
                        Gender = user.Gender,
                        DateOfBirth = user.DateOfBirth,
                        LanguagePreferences = user.LanguagePreferences,
                        Nationality = user.Nationality,
                        MobileOperator = user.MobileOperator,
                        PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                        EmailConfirmed = user.EmailConfirmed,
                        TwoFactorEnabled = user.TwoFactorEnabled,
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = user.UpdatedAt,
                        Roles = userRoles.ToList(),
                        Companies = user.Companies?.Select(c => new UserCompanyDto
                        {
                            Id = c.Id,
                            DisplayName = c.DisplayName
                        }).ToList() ?? new List<UserCompanyDto>(),
                        LegacyData = user.LegacyData != null ? new UserLegacyDataDto
                        {
                            Uid = user.LegacyData.Uid,
                            Alias = user.LegacyData.Alias ?? string.Empty,
                            Email = user.LegacyData.Email ?? string.Empty,
                            Name = user.LegacyData.Name ?? string.Empty,
                            Phone = user.LegacyData.Phone,
                            IsAdmin = user.LegacyData.IsAdmin,
                            Language = user.LegacyData.Language,
                            Roles = user.LegacyData.Roles
                        } : null
                    },
                    Subscriptions = new UserSubscriptionSummaryDto
                    {
                        TotalActiveSubscriptions = activeSubscriptions.Count,
                        TotalActiveFreeSubscriptions = activeFreeSubscriptions.Count,
                        TotalActiveP2PSubscriptions = activeP2PSubscriptions.Count,
                        TotalActiveAddons = activeAddons.Count(a => a.IsActive),
                        ActiveSubscriptions = subscriptionsByVertical,
                        ActiveFreeSubscriptions = freeSubscriptionsByVertical,
                        ActiveP2PSubscriptions = activeP2PSubscriptions,
                        ActiveAddons = addonsByVertical,
                        UsageSummary = usageSummary,
                        EarliestExpiryDate = GetEarliestExpiryDate(activeSubscriptions, activeFreeSubscriptions),
                        LatestExpiryDate = GetLatestExpiryDate(activeSubscriptions, activeFreeSubscriptions)
                    },
                    LastUpdated = DateTime.UtcNow,
                    ApiVersion = "V2"
                };

                return TypedResults.Ok(response);
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while fetching user details.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // Helper methods (add these to your AuthService class)
        private OverallUsageSummaryDto CalculateOverallUsageSummary(
            List<V2SubscriptionResponseDto> paidSubscriptions,
            List<V2SubscriptionResponseDto> freeSubscriptions,
            List<V2UserAddonResponseDto> addons)
        {
            var summary = new OverallUsageSummaryDto();

            // Calculate totals from paid subscriptions
            foreach (var subscription in paidSubscriptions)
            {
                if (subscription.Quota != null)
                {
                    summary.TotalAdsRemaining += Math.Max(0, (subscription.Quota.TotalAdsAllowed) - (subscription.Quota.AdsUsed));
                    summary.TotalPromotionsRemaining += Math.Max(0, (subscription.Quota.TotalPromotionsAllowed) - (subscription.Quota.PromotionsUsed));
                    summary.TotalFeaturesRemaining += Math.Max(0, (subscription.Quota.TotalFeaturesAllowed) - (subscription.Quota.FeaturesUsed));
                    summary.TotalRefreshesRemaining += Math.Max(0, (subscription.Quota.DailyRefreshesAllowed) - (subscription.Quota.DailyRefreshesUsed));
                }

                // Set vertical flags
                switch (subscription.Vertical)
                {
                    case Vertical.Classifieds:
                        summary.HasActiveClassifieds = true;
                        break;
                    case Vertical.Properties:
                        summary.HasActiveProperties = true;
                        break;
                    case Vertical.Services:
                        summary.HasActiveServices = true;
                        break;
                    case Vertical.Rewards:
                        summary.HasActiveRewards = true;
                        break;
                }
            }

            // Calculate totals from FREE subscriptions
            foreach (var freeSubscription in freeSubscriptions)
            {
                if (freeSubscription.Quota != null)
                {
                    summary.TotalFreeAdsRemaining += Math.Max(0, (freeSubscription.Quota.TotalAdsAllowed) - (freeSubscription.Quota.AdsUsed));
                }

                // Set vertical flags
                switch (freeSubscription.Vertical)
                {
                    case Vertical.Classifieds:
                        summary.HasActiveClassifieds = true;
                        break;
                    case Vertical.Properties:
                        summary.HasActiveProperties = true;
                        break;
                    case Vertical.Services:
                        summary.HasActiveServices = true;
                        break;
                    case Vertical.Rewards:
                        summary.HasActiveRewards = true;
                        break;
                }
            }

            return summary;
        }

        private DateTime? GetEarliestExpiryDate(List<V2SubscriptionResponseDto> paidSubscriptions, List<V2SubscriptionResponseDto> freeSubscriptions)
        {
            var allSubscriptions = paidSubscriptions.Concat(freeSubscriptions).Where(s => s.IsActive);
            return allSubscriptions.Any() ? allSubscriptions.Min(s => s.EndDate) : null;
        }

        private DateTime? GetLatestExpiryDate(List<V2SubscriptionResponseDto> paidSubscriptions, List<V2SubscriptionResponseDto> freeSubscriptions)
        {
            var allSubscriptions = paidSubscriptions.Concat(freeSubscriptions).Where(s => s.IsActive);
            return allSubscriptions.Any() ? allSubscriptions.Max(s => s.EndDate) : null;
        }

        private string GetCategoryPath(string category, string? l1Category, string? l2Category)
        {
            if (!string.IsNullOrEmpty(l2Category))
                return $"{category} > {l1Category} > {l2Category}";
            if (!string.IsNullOrEmpty(l1Category))
                return $"{category} > {l1Category}";
            return category;
        }

        // Helper methods to extract category parts from path
        private string ExtractCategoryFromPath(string categoryPath)
        {
            return categoryPath.Split(" > ")[0];
        }

        private string? ExtractL1CategoryFromPath(string categoryPath)
        {
            var parts = categoryPath.Split(" > ");
            return parts.Length > 1 ? parts[1] : null;
        }

        private string? ExtractL2CategoryFromPath(string categoryPath)
        {
            var parts = categoryPath.Split(" > ");
            return parts.Length > 2 ? parts[2] : null;
        }
    }
}
