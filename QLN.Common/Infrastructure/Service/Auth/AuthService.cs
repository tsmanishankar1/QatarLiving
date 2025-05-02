using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
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


        public async Task<Results<Ok<ApiResponse<string>>, BadRequest<ApiResponse<string>>, ValidationProblem, NotFound<ApiResponse<string>>, Conflict<ApiResponse<string>>, ProblemHttpResult>> Register(RegisterRequest request, HttpContext context)
        {
            try
            {
                if (!TempVerificationStore.VerifiedEmails.Contains(request.Emailaddress) ||
                    !TempVerificationStore.VerifiedPhoneNumbers.Contains(request.Mobilenumber))
                {
                    return TypedResults.BadRequest(new ApiResponse<string>
                    {
                        Status = false,
                        Message = "Please verify your Email and Phone Number before registering."
                    });
                }

                var existingUser = await _userManager.FindByEmailAsync(request.Emailaddress);
                if (existingUser != null)
                {
                    return TypedResults.NotFound(new ApiResponse<string>
                    {
                        Status = false,
                        Message = "Email is already registered. Please login."
                    });
                }

                var usernameExists = await _userManager.FindByNameAsync(request.Username);
                if (usernameExists != null)
                {
                    return TypedResults.BadRequest(new ApiResponse<string>
                    {
                        Status = false,
                        Message = $"Username '{request.Username}' is already taken."
                    });
                }

                var mobileRegex = new Regex(@"^(\+?\d{1,3})?[\s]?\d{10,15}$");
                if (!mobileRegex.IsMatch(request.Mobilenumber))
                {
                    return TypedResults.BadRequest(new ApiResponse<string>
                    {
                        Status = false,
                        Message = "Invalid mobile number format. Please enter a valid 10 to 15 digits."
                    });
                }

                var today = DateOnly.FromDateTime(DateTime.Today);
                var age = today.Year - request.Dateofbirth.Year;
                if (request.Dateofbirth > today.AddYears(-age)) age--;

                if (age < 18)
                {
                    return TypedResults.BadRequest(new ApiResponse<string>
                    {
                        Status = false,
                        Message = "You must be at least 18 years old to register."
                    });
                }

                var newUser = new ApplicationUser
                {
                    UserName = request.Username,
                    Email = request.Emailaddress,
                    PhoneNumber = request.Mobilenumber,
                    Firstname = request.FirstName,
                    Lastname = request.Lastname,
                    Dateofbirth = request.Dateofbirth,
                    Gender = request.Gender,
                    Mobileoperator = request.MobileOperator,
                    Nationality = request.Nationality,
                    Languagepreferences = request.Languagepreferences,
                    IsCompany = false,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    Isactive = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    Createdat = DateTime.UtcNow,

                };

                var createResult = await _userManager.CreateAsync(newUser, request.Password);

                if (!createResult.Succeeded)
                {
                    var errors = createResult.Errors
                        .GroupBy(e => e.Code)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

                    return TypedResults.ValidationProblem(errors,
                        detail: "One or more errors occurred during registration.");
                }

                if (!await _roleManager.RoleExistsAsync("User"))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>("User"));
                }

                await _userManager.AddToRoleAsync(newUser, "User");

                TempVerificationStore.VerifiedEmails.Remove(request.Emailaddress);
                TempVerificationStore.VerifiedPhoneNumbers.Remove(request.Mobilenumber);

                return TypedResults.Ok(new ApiResponse<string>
                {
                    Status = true,
                    Message = "User registered successfully."
                });
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }


        public async Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, BadRequest<ApiResponse<string>>>> SendEmailOtp(string email)
        {
            try
            {
                var existingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == email && u.Isactive == true);
                if (existingUser != null)
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Email already registered"));
                }

                var otp = new Random().Next(100000, 999999).ToString();

                TempVerificationStore.EmailOtps[email] = otp;

                await _emailSender.SendOtpEmailAsync(email, otp);

                return TypedResults.Ok(ApiResponse<string>.Success("OTP sent to your email."));
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }


        public async Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, BadRequest<ApiResponse<string>>>> VerifyEmailOtp(string email, string otp)
        {
            try
            {
                if (!TempVerificationStore.EmailOtps.TryGetValue(email, out var storedOtp))
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("OTP not requested or expired."));
                }

                if (storedOtp != otp)
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Invalid or expired OTP."));
                }

                TempVerificationStore.VerifiedEmails.Add(email);


                TempVerificationStore.EmailOtps.Remove(email);

                return TypedResults.Ok(ApiResponse<string>.Success("Email verified successfully."));
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }


        public async Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, BadRequest<ApiResponse<string>>>> SendPhoneOtp(string phoneNumber)
        {
            try
            {
                var existingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.Isactive == true);
                if (existingUser != null)
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Phone number already registered."));
                }

                var otp = new Random().Next(100000, 999999).ToString();


                TempVerificationStore.PhoneOtps[phoneNumber] = otp;

                string smsText = $"Your OTP for verification is {otp}.";

                string customerId = _config["OoredooSmsApi:CustomerId"];
                string userName = _config["OoredooSmsApi:UserName"];
                string userPassword = _config["OoredooSmsApi:UserPassword"];
                string originator = _config["OoredooSmsApi:Originator"];
                string apiUrl = _config["OoredooSmsApi:ApiUrl"];

                var response = await SendSms(apiUrl, customerId, userName, userPassword, phoneNumber, smsText, originator);

                if (response.IsSuccessStatusCode)
                {
                    return TypedResults.Ok(ApiResponse<string>.Success("OTP sent successfully to phone."));
                }
                else
                {
                    return TypedResults.Problem(
                        detail: "Failed to send OTP via SMS. Please try again later.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }


        public async Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, BadRequest<ApiResponse<string>>>> VerifyPhoneOtp(string phoneNumber, string otp)
        {
            try
            {
                if (!TempVerificationStore.PhoneOtps.TryGetValue(phoneNumber, out var storedOtp))
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("OTP not requested or expired."));
                }

                if (storedOtp != otp)
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Invalid or expired OTP."));
                }

                TempVerificationStore.VerifiedPhoneNumbers.Add(phoneNumber);


                TempVerificationStore.PhoneOtps.Remove(phoneNumber);

                return TypedResults.Ok(ApiResponse<string>.Success("Phone number verified successfully."));
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }


        public async Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, BadRequest<ApiResponse<string>>>> ForgotPassword(ForgotPasswordRequest request)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.Isactive == true);

                if (user != null && await _userManager.IsEmailConfirmedAsync(user))
                {
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var baseUrl = _config.GetSection("BaseUrl")["resetPassword"];

                    var resetUrl = $"{baseUrl}?email={Uri.EscapeDataString(request.Email)}&code={encodedCode}";

                    await _emailSender.SendPasswordResetLinkAsync(user, user.Email, resetUrl);

                    return TypedResults.Ok(ApiResponse<string>.Success("If your email is registered and confirmed, password reset link has been sent."));
                }
                else
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("User is not registered with this email or email is not confirmed."));
                }
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
            detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
            statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Results<Ok<ApiResponse<string>>, BadRequest<ApiResponse<string>>, NotFound<ApiResponse<string>>, ValidationProblem, ProblemHttpResult>> ResetPassword(ResetPasswordRequest request)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(r => r.Email == request.Email && r.Isactive == true);

                if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
                {
                    return TypedResults.NotFound(ApiResponse<string>.Success("User with this email is not registered or email not confirmed."));
                }

                var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.ResetCode));

                var isValidToken = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "ResetPassword", decodedCode);
                if (!isValidToken)
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Invalid or expired token."));
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
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
            detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
            statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Results<Ok<ApiResponse<LoginResponse>>, BadRequest<ApiResponse<string>>, UnauthorizedHttpResult, ProblemHttpResult, ValidationProblem>> Login(LoginRequest request)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                u.UserName == request.UsernameOrEmailOrPhone ||
                u.Email == request.UsernameOrEmailOrPhone ||
                u.PhoneNumber == request.UsernameOrEmailOrPhone && u.Isactive == true);


                if (user == null)
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Invalid credentials"));
                }

                else if (!await _userManager.CheckPasswordAsync(user, request.Password))
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Invalid credentials"));
                }


                if (user.TwoFactorEnabled)
                {
                 
                    return TypedResults.Ok(ApiResponse<LoginResponse>.Success("Two-Factor Authentication is enabled. Choose email or mobile to receive OTP.", new LoginResponse
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

                await _userManager.SetAuthenticationTokenAsync(user, Constants.ConstantValues.QLNProvider, Constants.ConstantValues.RefreshToken, refreshToken);
                await _userManager.SetAuthenticationTokenAsync(user, Constants.ConstantValues.QLNProvider, Constants.ConstantValues.RefreshTokenExpiry, DateTime.UtcNow.AddDays(7).ToString("o"));

                return TypedResults.Ok(ApiResponse<LoginResponse>.Success("Login successful", new LoginResponse
                {
                    Username = user.UserName,
                    Emailaddress = user.Email,
                    Mobilenumber = user.PhoneNumber,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                }));
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
           detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
           statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Results<Ok<ApiResponse<LoginResponse>>, BadRequest<ApiResponse<string>>, ProblemHttpResult, ValidationProblem>> Verify2FA(Verify2FARequest request)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                    u.UserName == request.UsernameOrEmailOrPhone ||
                    u.Email == request.UsernameOrEmailOrPhone ||
                    u.PhoneNumber == request.UsernameOrEmailOrPhone && u.Isactive == true);

                if (user == null)
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Invalid credentials."));
                }

                if (!user.TwoFactorEnabled)
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Two-Factor Authentication is not enabled for this user."));
                }

                var provider = request.Method.Equals(ConstantValues.Phone, StringComparison.OrdinalIgnoreCase)
                    ? TokenOptions.DefaultPhoneProvider
                    : TokenOptions.DefaultEmailProvider;

                var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, provider, request.TwoFactorCode);             

                if (!isValid)
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Invalid or expired 2FA code."));
                }

                var accessToken = await _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();

                await _userManager.SetAuthenticationTokenAsync(user, Constants.ConstantValues.QLNProvider, Constants.ConstantValues.RefreshToken, refreshToken);
                await _userManager.SetAuthenticationTokenAsync(user, Constants.ConstantValues.QLNProvider, Constants.ConstantValues.RefreshTokenExpiry, DateTime.UtcNow.AddDays(7).ToString("o"));

                return TypedResults.Ok(ApiResponse<LoginResponse>.Success("2FA verified. Login successful.", new LoginResponse
                {
                    Username = user.UserName,
                    Emailaddress = user.Email,
                    Mobilenumber = user.PhoneNumber,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                }));
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        public async Task<Results<Ok<ApiResponse<RefreshTokenResponse>>, BadRequest<ApiResponse<string>>, ProblemHttpResult, UnauthorizedHttpResult>> RefreshToken(RefreshTokenRequest request)
        {
            try
            {
                ApplicationUser? user = null;
                foreach (var u in _userManager.Users)
                {
                    var storedToken = await _userManager.GetAuthenticationTokenAsync(u, Constants.ConstantValues.RefreshToken, "refresh_token");
                    var expiryStr = await _userManager.GetAuthenticationTokenAsync(u, Constants.ConstantValues.RefreshTokenExpiry, "refresh_token_expiry");

                    if (storedToken == request.RefreshToken)
                    {
                        if (!DateTime.TryParse(expiryStr, out var expiry))
                        {
                            return TypedResults.BadRequest(ApiResponse<string>.Fail("Invalid refresh token expiry date."));
                        }

                        if (expiry <= DateTime.UtcNow)
                        {
                            return TypedResults.BadRequest(ApiResponse<string>.Fail("Refresh token has expired."));
                        }

                        user = u;
                        break;
                    }
                }

                if (user == null)
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Invalid refresh token."));
                }

                var newAccessToken = await _tokenService.GenerateAccessToken(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                await _userManager.SetAuthenticationTokenAsync(user, Constants.ConstantValues.QLNProvider, Constants.ConstantValues.RefreshToken, newRefreshToken);
                await _userManager.SetAuthenticationTokenAsync(user, Constants.ConstantValues.QLNProvider, Constants.ConstantValues.RefreshTokenExpiry, DateTime.UtcNow.AddDays(7).ToString("o"));

                return TypedResults.Ok(ApiResponse<RefreshTokenResponse>.Success("Token refreshed", new RefreshTokenResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken
                }));
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                   detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                   statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<IResult> Toggle2FA(TwoFactorToggleRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.EmailorPhoneNumber))
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Email or phone number is required."));
                }

                var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                u.Email == request.EmailorPhoneNumber || u.PhoneNumber == request.EmailorPhoneNumber && u.Isactive == true);

                if (user == null)
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("Invalid email or phone number."));
                }

                user.TwoFactorEnabled = request.Enable;
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    return TypedResults.BadRequest(ApiResponse<string>.Fail($"Failed to update user: {errors}"));
                }

                var status = request.Enable ? "enabled" : "disabled";
                return TypedResults.Ok(ApiResponse<string>.Success($"Two-Factor Authentication has been {status}."));
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                  detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                  statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<IResult> GetProfile(Guid Id)
        {
            try
            {
                if (Id == Guid.Empty)
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("email is required."));
                }

                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == Id && u.Isactive == true);

                if (user == null)
                {
                    return TypedResults.NotFound(ApiResponse<string>.Fail("User not Found"));
                }
                return TypedResults.Ok(ApiResponse<object>.Success("Profile data", new
                {
                    user.UserName,
                    user.Firstname,
                    user.Lastname,
                    user.Email,
                    user.PhoneNumber,
                    user.Gender,
                    user.Dateofbirth,
                    user.Languagepreferences,
                    user.Nationality,
                    user.Mobileoperator,
                    user.PhoneNumberConfirmed,
                    user.EmailConfirmed,
                    user.IsCompany,
                    user.TwoFactorEnabled
                }));
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<IResult> UpdateProfile(Guid id, UpdateProfileRequest request)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return TypedResults.BadRequest(ApiResponse<string>.Fail("User ID is required."));
                }

                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id && u.Isactive == true);


                if (user == null || user.Isactive == false)
                {
                    return TypedResults.NotFound(ApiResponse<string>.Fail("User not found"));
                }

                user.Firstname = request.FirstName;
                user.Lastname = request.LastName;
                user.Gender = request.Gender;
                user.Dateofbirth = request.Dateofbirth;
                user.Nationality = request.Nationality;
                user.PhoneNumber = request.MobileNumber;
                user.Languagepreferences = request.Languagepreferences;
                user.Updatedat = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                return TypedResults.Ok(ApiResponse<string>.Success("Profile updated successfully"));
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    detail: ApiResponse<string>.Fail("An unexpected error occurred. Please try again later.").Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<IResult> Logout(Guid id)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id && u.Isactive == true);

                if (user == null)
                {
                    return TypedResults.NotFound(ApiResponse<string>.Fail("User not found."));
                }


                await _userManager.RemoveAuthenticationTokenAsync(user, Constants.ConstantValues.QLNProvider, Constants.ConstantValues.RefreshToken);
                await _userManager.RemoveAuthenticationTokenAsync(user, Constants.ConstantValues.QLNProvider, Constants.ConstantValues.RefreshTokenExpiry);


                await _httpContextAccessor.HttpContext!.SignOutAsync();

                return TypedResults.Ok(ApiResponse<string>.Success("User logged out successfully."));
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                return TypedResults.Problem(
                    detail: ApiResponse<string>.Fail("An unexpected error occurred during logout.").Message,
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

        public async Task<ApiResponse<string>> SendTwoFactorOtp(Send2FARequest request)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                (u.UserName == request.UsernameOrEmailOrPhone ||
                 u.Email == request.UsernameOrEmailOrPhone ||
                 u.PhoneNumber == request.UsernameOrEmailOrPhone) &&
                 u.Isactive == true);

            if (user == null)
                return ApiResponse<string>.Fail("User not found.");

            if (!user.TwoFactorEnabled)
                return ApiResponse<string>.Fail("Two-Factor Authentication is not enabled for this user.");

            var method = request.Method?.Trim().ToLowerInvariant();

            if (method == Constants.ConstantValues.Phone.ToLowerInvariant())
            {
                if (string.IsNullOrWhiteSpace(user.PhoneNumber) || !user.PhoneNumberConfirmed)
                    return ApiResponse<string>.Fail("Phone number is not set or confirmed.");

                var otp = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);
                var smsText = $"Your 2FA OTP is {otp}";

                var response = await SendSms(
                    _config["OoredooSmsApi:ApiUrl"],
                    _config["OoredooSmsApi:CustomerId"],
                    _config["OoredooSmsApi:UserName"],
                    _config["OoredooSmsApi:UserPassword"],
                    user.PhoneNumber,smsText,
                    _config["OoredooSmsApi:Originator"]
                );

                return response.IsSuccessStatusCode
                    ? ApiResponse<string>.Success("2FA OTP sent via phone.")
                    : ApiResponse<string>.Fail("Failed to send OTP via SMS.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(user.Email) || !user.EmailConfirmed)
                    return ApiResponse<string>.Fail("Email is not set or confirmed.");

                var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
                await _emailSender.SendTwoFactorCode(user, user.Email, code);

                return ApiResponse<string>.Success("2FA OTP sent via email.");
            }
        }

    }
}
