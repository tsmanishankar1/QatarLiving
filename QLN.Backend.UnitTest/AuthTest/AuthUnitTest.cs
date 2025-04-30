using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable.Moq;
using Moq;
using QLN.Backend.UnitTest.IService;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.ITokenService;
using QLN.Common.Infrastructure.Model;
using System.Text;
using System.Text.Encodings.Web;

namespace QLN.Backend.UnitTest.AuthTest
{
    public class AuthUnitTest
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IEmailSender<ApplicationUser>> _mockEmailSender;
        private readonly Mock<LinkGenerator> _mockLinkGenerator;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<ILinkGeneratorWrapper> _mockLinkGeneratorWrapper = new();

        public AuthUnitTest()
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var options = new Mock<IOptions<IdentityOptions>>();
            var passwordHasher = new Mock<IPasswordHasher<ApplicationUser>>();
            var userValidators = new List<IUserValidator<ApplicationUser>>();
            var passwordValidators = new List<IPasswordValidator<ApplicationUser>>();
            var keyNormalizer = new Mock<ILookupNormalizer>();
            var errors = new Mock<IdentityErrorDescriber>();
            var services = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<UserManager<ApplicationUser>>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object,
            options.Object,
            passwordHasher.Object,
            userValidators,
            passwordValidators,
            keyNormalizer.Object,
            errors.Object,
            services.Object,
            logger.Object);
            _mockEmailSender = new Mock<IEmailSender<ApplicationUser>>();
            _mockLinkGenerator = new Mock<LinkGenerator>();
            _mockHttpContext = new Mock<HttpContext>();
        }
        //Register
        [Fact]
        public async Task RegisterInvalidRequest()
        {
            var request = new RegisterRequest
            {
                Username = "testuser",
                FirstName = "Test",
                Lastname = "User",
                Dateofbirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25)),
                Gender = "Male",
                MobileOperator = "TestOperator",
                Mobilenumber = "1234567890",
                Emailaddress = "test@example.com",
                Nationality = "TestNation",
                Languagepreferences = "English",
                Location = "TestLocation",
                Password = "weak"
            };

            var errors = new List<IdentityError>
            {
                new IdentityError { Code = "PasswordTooShort", Description = "Password is too short" },
                new IdentityError { Code = "PasswordRequiresNonAlphanumeric", Description = "Password requires non-alphanumeric characters" }
            };

            _mockUserManager
                .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Failed(errors.ToArray()));
            var result = await AuthUnitTest.InvokeRegisterEndpoint(
                request,
                _mockHttpContext.Object,
                _mockUserManager.Object,
                _mockEmailSender.Object,
                _mockLinkGenerator.Object);
            Assert.NotNull(result);
            var validationProblem = result as ValidationProblem;
            Assert.NotNull(validationProblem);
            Assert.Contains("PasswordTooShort", validationProblem.ProblemDetails.Errors.Keys);
            Assert.Contains("PasswordRequiresNonAlphanumeric", validationProblem.ProblemDetails.Errors.Keys);
            _mockEmailSender.Verify(x => x.SendConfirmationLinkAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never());
        }
        [Fact]
        public async Task RegisterNullConfirmationUrlDoesNotSendEmail()
        {
            var request = new RegisterRequest
            {
                Username = "testuser",
                FirstName = "Test",
                Lastname = "User",
                Dateofbirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25)),
                Gender = "Male",
                MobileOperator = "TestOperator",
                Mobilenumber = "1234567890",
                Emailaddress = "test@example.com",
                Nationality = "TestNation",
                Languagepreferences = "English",
                Location = "TestLocation",
                Password = "Password123!"
            };

            _mockUserManager
                .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager
                .Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("confirmation-token");
            var result = await AuthUnitTest.InvokeRegisterEndpoint(
                request,
                _mockHttpContext.Object,
                _mockUserManager.Object,
                _mockEmailSender.Object,
                _mockLinkGenerator.Object);

            Assert.NotNull(result);
            var okResult = result as Ok<ApiResponse<string>>;
            Assert.NotNull(okResult);
            Assert.Equal("User registered successfully. Please check your email to confirm your account.", okResult.Value.Message);

            _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password), Times.Once());
            _mockEmailSender.Verify(x => x.SendConfirmationLinkAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never());
        }
        public static async Task<IResult> InvokeRegisterEndpoint(
            RegisterRequest request,
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender<ApplicationUser> emailSender,
            LinkGenerator linkGenerator)
        {
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
        }
        //Confirmation Email
        [Fact]
        public static async Task ConfirmEmailWithValidParametersReturnsHtmlResponse()
        {
            var userId = Guid.NewGuid();
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("validCode"));
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManager = MockUserManager(userStore.Object);
            var user = new ApplicationUser { Id = Guid.NewGuid() };
            userManager.Setup(um => um.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);
            userManager.Setup(um => um.ConfirmEmailAsync(user, "validCode"))
                .ReturnsAsync(IdentityResult.Success);
            var result = await InvokeEndpoint(userId, code, userManager.Object);
            Assert.IsType<ContentHttpResult>(result);
            var contentResult = (ContentHttpResult)result;
            Assert.Equal("<h2>Email Confirmed Successfully</h2>", contentResult.ResponseContent);
            Assert.Equal("text/html", contentResult.ContentType);
        }

        [Fact]
        public static async Task ConfirmEmailWithInvalidUserReturnsNotFound()
        {
            var userId = Guid.NewGuid();
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("validCode"));
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManager = MockUserManager(userStore.Object);

            userManager.Setup(um => um.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((ApplicationUser)null);
            var result = await InvokeEndpoint(userId, code, userManager.Object);
            Assert.IsType<NotFound<string>>(result);
            var notFoundResult = (NotFound<string>)result;
            Assert.Equal("User not Fouund", notFoundResult.Value);
        }

        [Fact]
        public static async Task ConfirmEmailWithInvalidCodeReturnsValidationProblem()
        {

            var userId = Guid.NewGuid();
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("invalidCode"));
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManager = MockUserManager(userStore.Object);

            var user = new ApplicationUser { Id = Guid.NewGuid() };
            var errors = new List<IdentityError>
            {
                new IdentityError { Code = "InvalidToken", Description = "The token is invalid." }
            };

            userManager.Setup(um => um.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            userManager.Setup(um => um.ConfirmEmailAsync(user, "invalidCode"))
                .ReturnsAsync(IdentityResult.Failed(errors.ToArray()));
            var result = await InvokeEndpoint(userId, code, userManager.Object);
            Assert.IsType<ValidationProblem>(result);
            var validationResult = (ValidationProblem)result;
            Assert.Contains("InvalidToken", validationResult.ProblemDetails.Errors.Keys);
            Assert.Equal(new[] { "The token is invalid." }, validationResult.ProblemDetails.Errors["InvalidToken"]);
        }
        //HelperMethod for confrimation email
        private static async Task<IResult> InvokeEndpoint(Guid userId, string code, UserManager<ApplicationUser> userManager)
        {
            var endpoint = new Func<Guid, string, UserManager<ApplicationUser>, Task<IResult>>(
                async (id, c, um) =>
                {
                    var user = await um.FindByIdAsync(id.ToString());
                    if (user == null)
                    {
                        return TypedResults.NotFound("User not Fouund");
                    }
                    var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(c));
                    var result = await um.ConfirmEmailAsync(user, decodedCode);
                    if (!result.Succeeded)
                    {
                        var errors = result.Errors
                            .GroupBy(e => e.Code)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
                        return TypedResults.ValidationProblem(errors);
                    }
                    return Results.Text("<h2>Email Confirmed Successfully</h2>", "text/html");
                });

            return await endpoint(userId, code, userManager);
        }
        //Forgot Password
        [Fact]
        public static async Task ForgotPasswordWithRegisteredAndConfirmedEmailSendsResetCode()
        {

            var request = new ForgotPasswordRequest { Email = "test@example.com" };
            var user = new ApplicationUser { Email = request.Email, EmailConfirmed = true };
            var resetToken = "ResetToken123";
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));

            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManager = MockUserManager(userStore.Object);
            var emailSender = new Mock<IEmailSender<ApplicationUser>>();

            userManager.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            userManager.Setup(um => um.IsEmailConfirmedAsync(user))
                .ReturnsAsync(true);

            userManager.Setup(um => um.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync(resetToken);

            emailSender.Setup(es => es.SendPasswordResetCodeAsync(
                    user,
                    user.Email,
                    It.Is<string>(s => s == encodedToken)))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var result = await InvokeEndpoint(request, userManager.Object, emailSender.Object);
            emailSender.Verify();
            Assert.IsType<Ok<ApiResponse<string>>>(result);
        }

        [Fact]
        public static async Task ForgotPasswordWithUnregisteredEmailReturnsSuccessResponse()
        {
            var request = new ForgotPasswordRequest { Email = "nonexistent@example.com" };
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManager = MockUserManager(userStore.Object);
            var emailSender = new Mock<IEmailSender<ApplicationUser>>();

            userManager.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser)null);
            var result = await InvokeEndpoint(request, userManager.Object, emailSender.Object);
            emailSender.Verify(es => es.SendPasswordResetCodeAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.Never);

            Assert.IsType<Ok<ApiResponse<string>>>(result);
        }

        [Fact]
        public static async Task ForgotPasswordWithUnconfirmedEmailReturnsSuccessResponse()
        {
            var request = new ForgotPasswordRequest { Email = "unconfirmed@example.com" };
            var user = new ApplicationUser { Email = request.Email, EmailConfirmed = false };

            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManager = MockUserManager(userStore.Object);
            var emailSender = new Mock<IEmailSender<ApplicationUser>>();

            userManager.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            userManager.Setup(um => um.IsEmailConfirmedAsync(user))
                .ReturnsAsync(false);
            var result = await InvokeEndpoint(request, userManager.Object, emailSender.Object);
            emailSender.Verify(es => es.SendPasswordResetCodeAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.Never);

            Assert.IsType<Ok<ApiResponse<string>>>(result);
        }
        //HelperMethod for ForgotPassword 
        private static async Task<Ok<ApiResponse<string>>> InvokeEndpoint(
            ForgotPasswordRequest request,
            UserManager<ApplicationUser> userManager,
            IEmailSender<ApplicationUser> emailSender)
        {
            var endpoint = new Func<ForgotPasswordRequest, UserManager<ApplicationUser>, IEmailSender<ApplicationUser>, Task<Ok<ApiResponse<string>>>>(
                async (req, um, es) =>
                {
                    var user = await um.FindByEmailAsync(req.Email);
                    if (user != null && await um.IsEmailConfirmedAsync(user))
                    {
                        var code = await um.GeneratePasswordResetTokenAsync(user);
                        var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        await es.SendPasswordResetCodeAsync(user, user.Email, encodedCode);
                    }
                    return TypedResults.Ok(ApiResponse<string>.Success("If your email is registered and confirmed, a reset code has been sent."));
                });

            return await endpoint(request, userManager, emailSender);
        }
        private static Mock<UserManager<ApplicationUser>> MockUserManager(IUserStore<ApplicationUser> store)
        {
            var userManager = new Mock<UserManager<ApplicationUser>>(
                store, null, null, null, null, null, null, null, null);
            userManager.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
            userManager.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());
            return userManager;
        }

        //RefreshToken
        [Fact]
        public static async Task RefreshTokenSuccessReturnsNewTokens()
        {
            var mockUserManager = MockUserManager<ApplicationUser>();
            var tokenService = new Mock<ITokenService>();
            var validRefreshToken = "valid-refresh-token";
            var newAccessToken = "new-access-token";
            var newRefreshToken = "new-refresh-token";

            var user = new ApplicationUser
            {
                UserName = "testuser",
                Email = "test@example.com"
            };

            var usersDbSet = new List<ApplicationUser> { user }.AsQueryable().BuildMockDbSet();
            mockUserManager.Setup(m => m.Users).Returns(usersDbSet.Object);

            mockUserManager.Setup(m => m.GetAuthenticationTokenAsync(
                user, ConstantValues.RefreshToken, "refresh_token"))
                .ReturnsAsync(validRefreshToken);

            mockUserManager.Setup(m => m.GetAuthenticationTokenAsync(
                user, ConstantValues.RefreshTokenExpiry, "refresh_token_expiry"))
                .ReturnsAsync(DateTime.UtcNow.AddMinutes(30).ToString("o"));

            mockUserManager.Setup(m => m.SetAuthenticationTokenAsync(
                user, ConstantValues.QLNProvider, ConstantValues.RefreshToken, newRefreshToken))
                .ReturnsAsync(IdentityResult.Success);

            mockUserManager.Setup(m => m.SetAuthenticationTokenAsync(
                user, ConstantValues.QLNProvider, ConstantValues.RefreshTokenExpiry, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            mockUserManager.Setup(m => m.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            tokenService.Setup(t => t.GenerateAccessToken(user)).ReturnsAsync(newAccessToken);
            tokenService.Setup(t => t.GenerateRefreshToken()).Returns(newRefreshToken);

            var request = new RefreshTokenRequest
            {
                RefreshToken = validRefreshToken
            };

            var result = await new Func<
                RefreshTokenRequest,
                UserManager<ApplicationUser>,
                ITokenService,
                Task<Results<Ok<ApiResponse<RefreshTokenResponse>>, UnauthorizedHttpResult>>
            >(async (req, userManager, tokenService) =>
            {
                ApplicationUser? matchedUser = null;

                foreach (var u in userManager.Users)
                {
                    var storedToken = await userManager.GetAuthenticationTokenAsync(u, ConstantValues.RefreshToken, "refresh_token");
                    var expiryStr = await userManager.GetAuthenticationTokenAsync(u, ConstantValues.RefreshTokenExpiry, "refresh_token_expiry");

                    if (storedToken == req.RefreshToken &&
                        DateTime.TryParse(expiryStr, out var expiry) &&
                        expiry > DateTime.UtcNow)
                    {
                        matchedUser = u;
                        break;
                    }
                }

                if (matchedUser == null)
                    return TypedResults.Unauthorized();

                var accessToken = await tokenService.GenerateAccessToken(matchedUser);
                var refreshToken = tokenService.GenerateRefreshToken();

                await userManager.SetAuthenticationTokenAsync(matchedUser, ConstantValues.QLNProvider, ConstantValues.RefreshToken, refreshToken);
                await userManager.SetAuthenticationTokenAsync(matchedUser, ConstantValues.QLNProvider, ConstantValues.RefreshTokenExpiry, DateTime.UtcNow.AddDays(7).ToString("o"));

                return TypedResults.Ok(ApiResponse<RefreshTokenResponse>.Success("Token refreshed", new RefreshTokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                }));
            })(request, mockUserManager.Object, tokenService.Object);

            if (result.Result is Ok<ApiResponse<RefreshTokenResponse>> okResult)
            {
                Assert.Equal("Token refreshed", okResult.Value.Message);
                Assert.Equal(newAccessToken, okResult.Value.Data.AccessToken);
                Assert.Equal(newRefreshToken, okResult.Value.Data.RefreshToken);
            }
            else
            {
                Assert.Fail("Expected Ok<ApiResponse<RefreshTokenResponse>>, but got something else.");
            }
        }

        [Fact]
        public static async Task RefreshTokenInvalidTokenReturnsUnauthorized()
        {
            var mockUserManager = MockUserManager<ApplicationUser>();
            var tokenService = new Mock<ITokenService>();
            var invalidToken = "invalid-token";

            var user = new ApplicationUser { UserName = "fakeuser", Email = "fake@example.com" };

            var usersDbSet = new List<ApplicationUser> { user }.AsQueryable().BuildMockDbSet();
            mockUserManager.Setup(m => m.Users).Returns(usersDbSet.Object);
            mockUserManager.Setup(m => m.GetAuthenticationTokenAsync(
                user, ConstantValues.RefreshToken, "refresh_token"))
                .ReturnsAsync("some-other-token");

            mockUserManager.Setup(m => m.GetAuthenticationTokenAsync(
                user, ConstantValues.RefreshTokenExpiry, "refresh_token_expiry"))
                .ReturnsAsync(DateTime.UtcNow.AddMinutes(30).ToString("o"));

            var request = new RefreshTokenRequest { RefreshToken = invalidToken };

            var result = await new Func<
                RefreshTokenRequest,
                UserManager<ApplicationUser>,
                ITokenService,
                Task<Results<Ok<ApiResponse<RefreshTokenResponse>>, UnauthorizedHttpResult>>
            >(async (req, userManager, tokenService) =>
            {
                ApplicationUser? matchedUser = null;

                foreach (var u in userManager.Users)
                {
                    var storedToken = await userManager.GetAuthenticationTokenAsync(u, ConstantValues.RefreshToken, "refresh_token");
                    var expiryStr = await userManager.GetAuthenticationTokenAsync(u, ConstantValues.RefreshTokenExpiry, "refresh_token_expiry");

                    if (storedToken == req.RefreshToken &&
                        DateTime.TryParse(expiryStr, out var expiry) &&
                        expiry > DateTime.UtcNow)
                    {
                        matchedUser = u;
                        break;
                    }
                }

                if (matchedUser == null)
                    return TypedResults.Unauthorized();

                return TypedResults.Ok(ApiResponse<RefreshTokenResponse>.Success("Should not reach here", null));
            })(request, mockUserManager.Object, tokenService.Object);

            Assert.IsType<UnauthorizedHttpResult>(result.Result);
        }

        //Helper Method for RefreshToken
        public static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            return new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);
        }
        //manage/info 
        [Fact]
        public static async Task UpdateProfileValidUserUpdatesAndReturnsOk()
        {
            var request = new UpdateProfileRequest
            {
                UsernameOrEmailOrPhone = "testuser",
                FirstName = "John",
                LastName = "Doe",
                MobileNumber = "1234567890",
                Location = "Doha"
            };

            var user = new ApplicationUser
            {
                UserName = "testuser",
                Email = "testuser@example.com",
                PhoneNumber = "1234567890"
            };

            var userList = new List<ApplicationUser> { user }.AsQueryable();

            var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null
            );

            var mockUserDbSet = new Mock<DbSet<ApplicationUser>>();
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Provider).Returns(userList.Provider);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Expression).Returns(userList.Expression);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.ElementType).Returns(userList.ElementType);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.GetEnumerator()).Returns(userList.GetEnumerator());

            mockUserManager.Setup(x => x.Users).Returns(mockUserDbSet.Object);
            mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                           .ReturnsAsync(IdentityResult.Success);

            var result = await new Func<UpdateProfileRequest, UserManager<ApplicationUser>, Task<IResult>>(async (req, um) =>
            {
                var foundUser = um.Users.FirstOrDefault(u =>
                    u.UserName == req.UsernameOrEmailOrPhone ||
                    u.Email == req.UsernameOrEmailOrPhone ||
                    u.PhoneNumber == req.UsernameOrEmailOrPhone);

                if (foundUser == null)
                    return TypedResults.Unauthorized();

                foundUser.Firstname = req.FirstName;
                foundUser.Lastname = req.LastName;
                foundUser.PhoneNumber = req.MobileNumber;
                foundUser.Location = req.Location;
                await um.UpdateAsync(foundUser);

                return TypedResults.Ok(ApiResponse<string>.Success("Profile updated successfully"));
            })(request, mockUserManager.Object);

            var okResult = Assert.IsType<Ok<ApiResponse<string>>>(result);
            Assert.Equal("Profile updated successfully", okResult.Value.Message);
        }

        [Fact]
        public static async Task UpdateProfileUserNotFoundReturnsUnauthorized()
        {
            var request = new UpdateProfileRequest
            {
                UsernameOrEmailOrPhone = "nonexistentuser",
                FirstName = "Jane",
                LastName = "Smith",
                MobileNumber = "9999999999",
                Location = "Doha"
            };

            var emptyUserList = new List<ApplicationUser>().AsQueryable();

            var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null
            );

            var mockUserDbSet = new Mock<DbSet<ApplicationUser>>();
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Provider).Returns(emptyUserList.Provider);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Expression).Returns(emptyUserList.Expression);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.ElementType).Returns(emptyUserList.ElementType);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.GetEnumerator()).Returns(emptyUserList.GetEnumerator());

            mockUserManager.Setup(x => x.Users).Returns(mockUserDbSet.Object);

            var result = await new Func<UpdateProfileRequest, UserManager<ApplicationUser>, Task<IResult>>(async (req, um) =>
            {
                var foundUser = um.Users.FirstOrDefault(u =>
                    u.UserName == req.UsernameOrEmailOrPhone ||
                    u.Email == req.UsernameOrEmailOrPhone ||
                    u.PhoneNumber == req.UsernameOrEmailOrPhone);

                if (foundUser == null)
                    return TypedResults.Unauthorized();

                foundUser.Firstname = req.FirstName;
                foundUser.Lastname = req.LastName;
                foundUser.PhoneNumber = req.MobileNumber;
                foundUser.Location = req.Location;
                await um.UpdateAsync(foundUser);

                return TypedResults.Ok(ApiResponse<string>.Success("Profile updated successfully"));
            })(request, mockUserManager.Object);
            Assert.IsType<UnauthorizedHttpResult>(result);
        }
        //get manage info 
        [Fact]
        public static async Task GetProfileUserFoundReturnsOkWithProfile()
        {
            var identity = "jane@example.com";

            var user = new ApplicationUser
            {
                UserName = "jane.doe",
                Email = "jane@example.com",
                PhoneNumber = "1234567890",
                Gender = "Female",
                Dateofbirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25)),
                Location = "Doha",
                Isactive = true,
                TwoFactorEnabled = true
            };

            var users = new List<ApplicationUser> { user }.AsQueryable();

            var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null
            );

            var mockUserDbSet = new Mock<DbSet<ApplicationUser>>();
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Provider).Returns(users.Provider);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Expression).Returns(users.Expression);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.ElementType).Returns(users.ElementType);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

            mockUserManager.Setup(x => x.Users).Returns(mockUserDbSet.Object);
            var result = await new Func<string, UserManager<ApplicationUser>, Task<IResult>>(async (email, um) =>
            {
                var foundUser = um.Users.FirstOrDefault(u => u.Email == email);
                if (foundUser == null)
                    return TypedResults.Unauthorized();

                return TypedResults.Ok(ApiResponse<object>.Success("Profile data", new
                {
                    foundUser.UserName,
                    foundUser.Email,
                    foundUser.PhoneNumber,
                    foundUser.Gender,
                    foundUser.Dateofbirth,
                    foundUser.Location,
                    foundUser.Isactive,
                    foundUser.TwoFactorEnabled
                }));
            })(identity, mockUserManager.Object);
            var okResult = Assert.IsType<Ok<ApiResponse<object>>>(result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.Equal("Profile data", response.Message);
        }
        [Fact]
        public static async Task GetProfileUserNotFoundReturnsUnauthorized()
        {
            var identity = "notfound@example.com";
            var emptyUsers = new List<ApplicationUser>().AsQueryable();
            var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null
            );

            var mockUserDbSet = new Mock<DbSet<ApplicationUser>>();
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Provider).Returns(emptyUsers.Provider);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Expression).Returns(emptyUsers.Expression);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.ElementType).Returns(emptyUsers.ElementType);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.GetEnumerator()).Returns(emptyUsers.GetEnumerator());
            mockUserManager.Setup(x => x.Users).Returns(mockUserDbSet.Object);
            var result = await new Func<string, UserManager<ApplicationUser>, Task<IResult>>(async (email, um) =>
            {
                var foundUser = um.Users.FirstOrDefault(u => u.Email == email);
                if (foundUser == null)
                    return TypedResults.Unauthorized();

                return TypedResults.Ok(ApiResponse<object>.Success("Profile data", foundUser));
            })(identity, mockUserManager.Object);
            Assert.IsType<UnauthorizedHttpResult>(result);
        }
        //manage 2fa
        [Fact]
        public static async Task TwoFactorAuthUserFoundUpdatesTwoFactorReturnsOk()
        {
            var request = new TwoFactorToggleRequest
            {
                EmailorPhoneNumber = "user@example.com",
                Enable = true
            };

            var user = new ApplicationUser
            {
                Email = "user@example.com",
                PhoneNumber = "1234567890",
                TwoFactorEnabled = false
            };

            var users = new List<ApplicationUser> { user }.AsQueryable();

            var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null
            );

            var mockUserDbSet = new Mock<DbSet<ApplicationUser>>();
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Provider).Returns(users.Provider);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Expression).Returns(users.Expression);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.ElementType).Returns(users.ElementType);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());
            mockUserManager.Setup(x => x.Users).Returns(mockUserDbSet.Object);
            mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
            var result = await new Func<TwoFactorToggleRequest, UserManager<ApplicationUser>, Task<IResult>>(async (req, um) =>
            {
                var user = um.Users.FirstOrDefault(u =>
                    u.Email == req.EmailorPhoneNumber || u.PhoneNumber == req.EmailorPhoneNumber);
                if (user == null)
                    return TypedResults.Unauthorized();

                user.TwoFactorEnabled = req.Enable;
                await um.UpdateAsync(user);

                var status = req.Enable ? "enabled" : "disabled";
                return TypedResults.Ok(ApiResponse<string>.Success($"Two-Factor Authentication has been {status}."));
            })(request, mockUserManager.Object);
            var okResult = Assert.IsType<Ok<ApiResponse<string>>>(result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            Assert.Equal("Two-Factor Authentication has been enabled.", response.Message);
        }
        [Fact]
        public static async Task TwoFactorAuthUserNotFoundReturnsUnauthorized()
        {
            var request = new TwoFactorToggleRequest
            {
                EmailorPhoneNumber = "unknown@example.com",
                Enable = true
            };
            var users = new List<ApplicationUser>().AsQueryable();
            var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null
            );

            var mockUserDbSet = new Mock<DbSet<ApplicationUser>>();
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Provider).Returns(users.Provider);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Expression).Returns(users.Expression);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.ElementType).Returns(users.ElementType);
            mockUserDbSet.As<IQueryable<ApplicationUser>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());
            mockUserManager.Setup(x => x.Users).Returns(mockUserDbSet.Object);
            var result = await new Func<TwoFactorToggleRequest, UserManager<ApplicationUser>, Task<IResult>>(async (req, um) =>
            {
                var user = um.Users.FirstOrDefault(u =>
                    u.Email == req.EmailorPhoneNumber || u.PhoneNumber == req.EmailorPhoneNumber);
                if (user == null)
                    return TypedResults.Unauthorized();

                user.TwoFactorEnabled = req.Enable;
                await um.UpdateAsync(user);

                var status = req.Enable ? "enabled" : "disabled";
                return TypedResults.Ok(ApiResponse<string>.Success($"Two-Factor Authentication has been {status}."));
            })(request, mockUserManager.Object);
            Assert.IsType<UnauthorizedHttpResult>(result);
        }
        //Login
        [Fact]
        public static async Task LoginSuccessfulWithout2FAReturnsTokens()
        {
            var request = new LoginRequest
            {
                UsernameOrEmailOrPhone = "user@example.com",
                Password = "Test@123"
            };

            var user = new ApplicationUser
            {
                UserName = "user1",
                Email = "user@example.com",
                PhoneNumber = "1234567890",
                TwoFactorEnabled = false
            };

            var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = MockUserManagers(mockUserStore.Object);
            var mockSignInManager = MockSignInManager(mockUserManager.Object);
            var mockTokenService = new Mock<ITokenService>();
            var mockEmailSender = new Mock<IEmailSender<ApplicationUser>>();

            mockUserManager.Setup(x => x.Users)
                .Returns(new List<ApplicationUser> { user }.AsQueryable().BuildMockDbSet().Object);
            mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);
            mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            mockTokenService.Setup(x => x.GenerateAccessToken(user)).ReturnsAsync("access-token");
            mockTokenService.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
            mockUserManager.Setup(x => x.SetAuthenticationTokenAsync(
                user, ConstantValues.QLNProvider, ConstantValues.RefreshToken, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            mockUserManager.Setup(x => x.SetAuthenticationTokenAsync(
                user, ConstantValues.QLNProvider, ConstantValues.RefreshTokenExpiry, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await new Func<LoginRequest, UserManager<ApplicationUser>, SignInManager<ApplicationUser>, ITokenService, IEmailSender<ApplicationUser>, Task<IResult>>(
                async (req, userMgr, signInMgr, tokenSvc, emailSvc) =>
                {
                    var foundUser = userMgr.Users.FirstOrDefault(u =>
                        u.UserName == req.UsernameOrEmailOrPhone ||
                        u.Email == req.UsernameOrEmailOrPhone ||
                        u.PhoneNumber == req.UsernameOrEmailOrPhone);

                    if (foundUser == null || !await userMgr.CheckPasswordAsync(foundUser, req.Password))
                        return TypedResults.Unauthorized();

                    if (!await userMgr.IsEmailConfirmedAsync(foundUser))
                        return TypedResults.ValidationProblem(new Dictionary<string, string[]> {
                    { "Email", new[] { "Email not confirmed." } }
                        });

                    if (foundUser.TwoFactorEnabled)
                    {
                        var code = await userMgr.GenerateTwoFactorTokenAsync(foundUser, TokenOptions.DefaultEmailProvider);
                        await emailSvc.SendPasswordResetCodeAsync(foundUser, foundUser.Email, code);

                        return TypedResults.Ok(ApiResponse<LoginResponse>.Success("2FA code sent", new LoginResponse
                        {
                            Username = foundUser.UserName,
                            Emailaddress = foundUser.Email,
                            Mobilenumber = foundUser.PhoneNumber,
                            AccessToken = string.Empty,
                            RefreshToken = string.Empty
                        }));
                    }

                    var accessToken = await tokenSvc.GenerateAccessToken(foundUser);
                    var refreshToken = tokenSvc.GenerateRefreshToken();

                    await userMgr.SetAuthenticationTokenAsync(foundUser, ConstantValues.QLNProvider, ConstantValues.RefreshToken, refreshToken);
                    await userMgr.SetAuthenticationTokenAsync(foundUser, ConstantValues.QLNProvider, ConstantValues.RefreshTokenExpiry, DateTime.UtcNow.AddDays(7).ToString("o"));
                    await userMgr.UpdateAsync(foundUser);

                    return TypedResults.Ok(ApiResponse<LoginResponse>.Success("Login successful", new LoginResponse
                    {
                        Username = foundUser.UserName,
                        Emailaddress = foundUser.Email,
                        Mobilenumber = foundUser.PhoneNumber,
                        AccessToken = accessToken,
                        RefreshToken = refreshToken
                    }));
                })(request, mockUserManager.Object, mockSignInManager.Object, mockTokenService.Object, mockEmailSender.Object);

            var okResult = Assert.IsType<Ok<ApiResponse<LoginResponse>>>(result);
            Assert.Equal("Login successful", okResult.Value.Message);
            Assert.Equal("access-token", okResult.Value.Data.AccessToken);
            Assert.Equal("refresh-token", okResult.Value.Data.RefreshToken);
        }
        [Fact]
        public static async Task LoginFailsReturnsUnauthorized()
        {
            var request = new LoginRequest
            {
                UsernameOrEmailOrPhone = "user@example.com",
                Password = "WrongPassword"
            };

            var user = new ApplicationUser
            {
                UserName = "user1",
                Email = "user@example.com",
                PhoneNumber = "1234567890",
                TwoFactorEnabled = false
            };

            var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = MockUserManager(mockUserStore.Object);
            var mockSignInManager = MockSignInManager(mockUserManager.Object);
            var mockTokenService = new Mock<ITokenService>();
            var mockEmailSender = new Mock<IEmailSender<ApplicationUser>>();

            mockUserManager.Setup(x => x.Users).Returns(new List<ApplicationUser> { user }.AsQueryable().BuildMockDbSet().Object);
            mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(false); // Invalid password
            var result = await new Func<LoginRequest, UserManager<ApplicationUser>, SignInManager<ApplicationUser>, ITokenService, IEmailSender<ApplicationUser>, Task<IResult>>(
                async (req, userMgr, signInMgr, tokenSvc, emailSvc) =>
                {
                    var foundUser = userMgr.Users.FirstOrDefault(u =>
                        u.UserName == req.UsernameOrEmailOrPhone ||
                        u.Email == req.UsernameOrEmailOrPhone ||
                        u.PhoneNumber == req.UsernameOrEmailOrPhone);

                    if (foundUser == null || !await userMgr.CheckPasswordAsync(foundUser, req.Password))
                        return TypedResults.Unauthorized();

                    return TypedResults.Ok();
                })(request, mockUserManager.Object, mockSignInManager.Object, mockTokenService.Object, mockEmailSender.Object);
            Assert.IsType<UnauthorizedHttpResult>(result);
        }
        // Helper Method to Login
        private static Mock<UserManager<ApplicationUser>> MockUserManagers(IUserStore<ApplicationUser> store)
        {
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(store, null, null, null, null, null, null, null, null);

            mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
            mockUserManager.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(true);
            mockUserManager.Setup(x => x.IsEmailConfirmedAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(true);

            return mockUserManager;
        }
        private static Mock<SignInManager<ApplicationUser>> MockSignInManager(UserManager<ApplicationUser> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var options = new Mock<IOptions<IdentityOptions>>();
            var logger = new Mock<ILogger<SignInManager<ApplicationUser>>>();
            var schemes = new Mock<IAuthenticationSchemeProvider>();
            var confirmation = new Mock<IUserConfirmation<ApplicationUser>>();

            var signInManager = new Mock<SignInManager<ApplicationUser>>(
                userManager,
                contextAccessor.Object,
                userPrincipalFactory.Object,
                options.Object,
                logger.Object,
                schemes.Object,
                confirmation.Object
            );

            signInManager.Setup(x => x.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), It.IsAny<string>()))
                         .Returns(Task.CompletedTask);
            signInManager.Setup(x => x.PasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                         .ReturnsAsync(SignInResult.Success);

            return signInManager;
        }
        //verigy 2fa 
        [Fact]
        public static async Task Verify2FASuccessReturnsTokens()
        {
            var request = new Verify2FARequest
            {
                UsernameOrEmailOrPhone = "user@example.com",
                TwoFactorCode = "123456"
            };

            var user = new ApplicationUser
            {
                UserName = "user1",
                Email = "user@example.com",
                PhoneNumber = "1234567890",
                TwoFactorEnabled = true
            };

            var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = MockUserManager(mockUserStore.Object);
            var mockTokenService = new Mock<ITokenService>();

            mockUserManager.Setup(x => x.Users).Returns(new List<ApplicationUser> { user }.AsQueryable().BuildMockDbSet().Object);
            mockUserManager.Setup(x => x.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, request.TwoFactorCode))
                .ReturnsAsync(true);
            mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            mockUserManager.Setup(x => x.SetAuthenticationTokenAsync(user, ConstantValues.QLNProvider, ConstantValues.RefreshToken, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            mockUserManager.Setup(x => x.SetAuthenticationTokenAsync(user, ConstantValues.QLNProvider, ConstantValues.RefreshTokenExpiry, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            mockTokenService.Setup(x => x.GenerateAccessToken(user)).ReturnsAsync("access-token");
            mockTokenService.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");

            var result = await new Func<Verify2FARequest, UserManager<ApplicationUser>, ITokenService, Task<IResult>>(
                async (req, userMgr, tokenSvc) =>
                {
                    var foundUser = await userMgr.Users.FirstOrDefaultAsync(u =>
                        u.UserName == req.UsernameOrEmailOrPhone ||
                        u.Email == req.UsernameOrEmailOrPhone ||
                        u.PhoneNumber == req.UsernameOrEmailOrPhone);

                    if (foundUser == null)
                        return TypedResults.NotFound();

                    if (!foundUser.TwoFactorEnabled)
                        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                        {
                    { "2FA", new[] { "Two-Factor Authentication is not enabled for this user." } }
                        });

                    var isValid = await userMgr.VerifyTwoFactorTokenAsync(foundUser, TokenOptions.DefaultEmailProvider, req.TwoFactorCode);
                    if (!isValid)
                        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                        {
                    { "2FA", new[] { "Invalid two-factor authentication code." } }
                        });

                    var accessToken = await tokenSvc.GenerateAccessToken(foundUser);
                    var refreshToken = tokenSvc.GenerateRefreshToken();

                    await userMgr.SetAuthenticationTokenAsync(foundUser, ConstantValues.QLNProvider, ConstantValues.RefreshToken, refreshToken);
                    await userMgr.SetAuthenticationTokenAsync(foundUser, ConstantValues.QLNProvider, ConstantValues.RefreshTokenExpiry, DateTime.UtcNow.AddDays(7).ToString("o"));
                    await userMgr.UpdateAsync(foundUser);

                    return TypedResults.Ok(ApiResponse<LoginResponse>.Success("2FA verified. Login successful.", new LoginResponse
                    {
                        Username = foundUser.UserName,
                        Emailaddress = foundUser.Email,
                        Mobilenumber = foundUser.PhoneNumber,
                        AccessToken = accessToken,
                        RefreshToken = refreshToken
                    }));
                })(request, mockUserManager.Object, mockTokenService.Object);

            var okResult = Assert.IsType<Ok<ApiResponse<LoginResponse>>>(result);
            Assert.Equal("2FA verified. Login successful.", okResult.Value.Message);
            Assert.Equal("access-token", okResult.Value.Data.AccessToken);
            Assert.Equal("refresh-token", okResult.Value.Data.RefreshToken);
        }
        [Fact]
        public static async Task Verify2FAInvalidCodeReturnsValidationError()
        {
            var request = new Verify2FARequest
            {
                UsernameOrEmailOrPhone = "user@example.com",
                TwoFactorCode = "wrong-code"
            };

            var user = new ApplicationUser
            {
                UserName = "user1",
                Email = "user@example.com",
                PhoneNumber = "1234567890",
                TwoFactorEnabled = true
            };

            var mockUserManager = MockUserManager(new Mock<IUserStore<ApplicationUser>>().Object);
            mockUserManager.Setup(x => x.Users).Returns(new List<ApplicationUser> { user }.AsQueryable().BuildMockDbSet().Object);
            mockUserManager.Setup(x => x.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, request.TwoFactorCode))
                           .ReturnsAsync(false);

            var mockTokenService = new Mock<ITokenService>();
            var result = await new Func<Verify2FARequest, UserManager<ApplicationUser>, ITokenService, Task<IResult>>(
                async (req, userMgr, tokenSvc) =>
                {
                    var foundUser = userMgr.Users.FirstOrDefault(u =>
                        u.UserName == req.UsernameOrEmailOrPhone ||
                        u.Email == req.UsernameOrEmailOrPhone ||
                        u.PhoneNumber == req.UsernameOrEmailOrPhone);

                    if (foundUser == null)
                        return TypedResults.NotFound();

                    if (!foundUser.TwoFactorEnabled)
                        return TypedResults.ValidationProblem(new Dictionary<string, string[]> { { "2FA", new[] { "Two-Factor Authentication is not enabled for this user." } } });

                    var isValid = await userMgr.VerifyTwoFactorTokenAsync(foundUser, TokenOptions.DefaultEmailProvider, req.TwoFactorCode);
                    if (!isValid)
                        return TypedResults.ValidationProblem(new Dictionary<string, string[]> { { "2FA", new[] { "Invalid two-factor authentication code." } } });

                    return null!;
                })(request, mockUserManager.Object, mockTokenService.Object);
            var validationResult = Assert.IsType<ValidationProblem>(result);
            Assert.True(validationResult.ProblemDetails.Errors.ContainsKey("2FA"));
        }
        [Fact]
        public static async Task Verify2FANotEnabledReturnsValidationError()
        {
            var request = new Verify2FARequest
            {
                UsernameOrEmailOrPhone = "user@example.com",
                TwoFactorCode = "123456"
            };
            var user = new ApplicationUser
            {
                UserName = "user1",
                Email = "user@example.com",
                PhoneNumber = "1234567890",
                TwoFactorEnabled = false
            };

            var mockUserManager = MockUserManager(new Mock<IUserStore<ApplicationUser>>().Object);
            mockUserManager.Setup(x => x.Users).Returns(new List<ApplicationUser> { user }.AsQueryable().BuildMockDbSet().Object);
            var mockTokenService = new Mock<ITokenService>();
            var result = await new Func<Verify2FARequest, UserManager<ApplicationUser>, ITokenService, Task<IResult>>(
                async (req, userMgr, tokenSvc) =>
                {
                    var foundUser = userMgr.Users.FirstOrDefault(u =>
                        u.UserName == req.UsernameOrEmailOrPhone ||
                        u.Email == req.UsernameOrEmailOrPhone ||
                        u.PhoneNumber == req.UsernameOrEmailOrPhone);

                    if (foundUser == null)
                        return TypedResults.NotFound();

                    if (!foundUser.TwoFactorEnabled)
                        return TypedResults.ValidationProblem(new Dictionary<string, string[]> { { "2FA", new[] { "Two-Factor Authentication is not enabled for this user." } } });

                    return null!;
                })(request, mockUserManager.Object, mockTokenService.Object);
            var validationResult = Assert.IsType<ValidationProblem>(result);
            Assert.True(validationResult.ProblemDetails.Errors.ContainsKey("2FA"));
            Assert.Equal("Two-Factor Authentication is not enabled for this user.", validationResult.ProblemDetails.Errors["2FA"].First());
        }
        [Fact]
        public static async Task Verify2FAUserNotFoundReturnsNotFound()
        {
            var request = new Verify2FARequest
            {
                UsernameOrEmailOrPhone = "unknown@example.com",
                TwoFactorCode = "123456"
            };

            var mockUserManager = MockUserManager(new Mock<IUserStore<ApplicationUser>>().Object);
            mockUserManager.Setup(x => x.Users).Returns(new List<ApplicationUser>().AsQueryable().BuildMockDbSet().Object);
            var mockTokenService = new Mock<ITokenService>();
            var result = await new Func<Verify2FARequest, UserManager<ApplicationUser>, ITokenService, Task<IResult>>(
                async (req, userMgr, tokenSvc) =>
                {
                    var foundUser = userMgr.Users.FirstOrDefault(u =>
                        u.UserName == req.UsernameOrEmailOrPhone ||
                        u.Email == req.UsernameOrEmailOrPhone ||
                        u.PhoneNumber == req.UsernameOrEmailOrPhone);

                    if (foundUser == null)
                        return TypedResults.NotFound();

                    return null!;
                })(request, mockUserManager.Object, mockTokenService.Object);
            Assert.IsType<NotFound>(result);
        }
        //Resend Confirmation Email
        [Fact]
        public async Task ResendConfirmationEmailUserNotFoundReturnsNotFound()
        {
            var request = new ResendConfirmationEmailRequest { Email = "notfound@example.com" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser)null);

            var result = await InvokeResendConfirmationEmail(
                request,
                _mockHttpContext.Object,
                _mockUserManager.Object,
                _mockEmailSender.Object,
                _mockLinkGeneratorWrapper.Object);

            var notFound = Assert.IsType<NotFound<string>>(result);
            Assert.Equal("User not Found", notFound.Value);
        }

        [Fact]
        public async Task ResendConfirmationEmailValidRequestSendsEmail()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com" };
            var request = new ResendConfirmationEmailRequest { Email = user.Email };
            var token = "token123";
            var expectedUrl = $"https://example.com/confirm-email?userId={user.Id}&code={token}";

            _mockUserManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync(token);

            _mockLinkGeneratorWrapper
                .Setup(x => x.GetUriByName(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<string>(),
                    It.IsAny<HostString?>(),
                    It.IsAny<PathString?>(),
                    It.IsAny<FragmentString>(),
                    It.IsAny<LinkOptions>()))
                .Returns(expectedUrl);

            var result = await InvokeResendConfirmationEmail(
                request,
                _mockHttpContext.Object,
                _mockUserManager.Object,
                _mockEmailSender.Object,
                _mockLinkGeneratorWrapper.Object
            );
            var textResult = Assert.IsType<ContentHttpResult>(result);
            Assert.Equal("text/html", textResult.ContentType);
            Assert.Contains("Email Confirmed", textResult.ResponseContent);

            _mockEmailSender.Verify(
                x => x.SendConfirmationLinkAsync(user, user.Email, HtmlEncoder.Default.Encode(expectedUrl)),
                Times.Once);
        }

        [Fact]
        public async Task ResendConfirmationEmailConfirmationUrlNullDoesNotSendEmail()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com" };
            var request = new ResendConfirmationEmailRequest { Email = user.Email };
            var token = "test-token";

            _mockUserManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("token123");

            _mockLinkGeneratorWrapper
                .Setup(x => x.GetUriByName(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<string>(),
                    It.IsAny<HostString?>(),
                    It.IsAny<PathString?>(),
                    It.IsAny<FragmentString>(),
                    It.IsAny<LinkOptions>()))
                .Returns<string>(null);

            var result = await InvokeResendConfirmationEmail(
                request,
                _mockHttpContext.Object,
                _mockUserManager.Object,
                _mockEmailSender.Object,
                _mockLinkGeneratorWrapper.Object);

            var textResult = Assert.IsType<ContentHttpResult>(result);
            Assert.Equal("text/html", textResult.ContentType);
            Assert.Contains("Email Confirmed", textResult.ResponseContent);

            _mockEmailSender.Verify(x => x.SendConfirmationLinkAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        //Helper method for Resend Confirmation Email
        public static async Task<IResult> InvokeResendConfirmationEmail(
            ResendConfirmationEmailRequest request,
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender<ApplicationUser> emailSender,
            ILinkGeneratorWrapper linkGenerator)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return TypedResults.NotFound("User not Found");

            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var confirmUrl = linkGenerator.GetUriByName(context, "ConfirmEmail", new
            {
                userId = user.Id,
                code = encodedCode
            }, scheme: null, host: null, pathBase: null, fragment: FragmentString.Empty, options: null);

            if (confirmUrl != null)
            {
                await emailSender.SendConfirmationLinkAsync(user, user.Email, HtmlEncoder.Default.Encode(confirmUrl));
            }

            return Results.Text("<h2>Email Confirmed Successfully</h2>", "text/html");
        }
        //Reset password
        [Fact]
        public async Task ResetPasswordSuccessReturnsSuccessMessage()
        {
            var user = new ApplicationUser { Email = "test@example.com" };
            var request = new ResetPasswordRequest
            {
                Email = user.Email,
                ResetCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("reset-token")),
                NewPassword = "New@123"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.ResetPasswordAsync(user, "reset-token", request.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            var result = await AuthUnitTest.InvokeResetPasswordEndpoint(request, _mockUserManager.Object);

            var okResult = Assert.IsType<Ok<ApiResponse<string>>>(result.Result);
            Assert.Equal("Password has been reset successfully", okResult.Value.Message);
        }
        [Fact]
        public async Task ResetPasswordUserNotFoundOrEmailNotConfirmedReturnsGenericMessage()
        {
            var request = new ResetPasswordRequest
            {
                Email = "notfound@example.com",
                ResetCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("dummy")),
                NewPassword = "test"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser)null);

            var result = await AuthUnitTest.InvokeResetPasswordEndpoint(request, _mockUserManager.Object);

            var okResult = Assert.IsType<Ok<ApiResponse<string>>>(result.Result);
            Assert.Contains("you will receive a password reset link", okResult.Value.Message);
        }
        [Fact]
        public async Task ResetPasswordResetFailsReturnsValidationErrors()
        {
            var user = new ApplicationUser { Email = "test@example.com" };
            var request = new ResetPasswordRequest
            {
                Email = user.Email,
                ResetCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("reset-token")),
                NewPassword = "weak"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.ResetPasswordAsync(user, "reset-token", request.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort", Description = "Password too short." }));

            var result = await AuthUnitTest.InvokeResetPasswordEndpoint(request, _mockUserManager.Object);

            var validationResult = Assert.IsType<ValidationProblem>(result.Result);
            Assert.True(validationResult.ProblemDetails.Errors.ContainsKey("PasswordTooShort"));
        }
        //Helper Method for Reset Password
        public static async Task<Results<Ok<ApiResponse<string>>, ValidationProblem>> InvokeResetPasswordEndpoint(
                ResetPasswordRequest request,
                UserManager<ApplicationUser> userManager)
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user == null || !(await userManager.IsEmailConfirmedAsync(user)))
            {
                return TypedResults.Ok(ApiResponse<string>.Success(
                "If your email is registered, you will receive a password reset link shortly",
                null));
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.ResetCode));
            var resetResult = await userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

            if (resetResult.Succeeded)
            {
                return TypedResults.Ok(ApiResponse<string>.Success(
                "Password has been reset successfully",
                null));
            }
            else
            {
                var errors = new Dictionary<string, string[]>();
                foreach (var error in resetResult.Errors)
                {
                    errors[error.Code] = new[] { error.Description };
                }

                return TypedResults.ValidationProblem(errors);
            }
        }
    }
}
