using Microsoft.AspNetCore.Mvc;
using QLN.Common.Infrastructure.InputModels;
using QLN.Common.Infrastructure.ServiceInterface;

namespace QLN.Backend.API.Controller.AuthController
{
    /// <summary>
    /// Controller responsible for authentication-related operations such as user registration and OTP handling.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="service">The authentication service used for handling auth operations.</param>
        public AuthController(IAuthService service)
        {
            _service = service;
        }

        /// <summary>
        /// Creates a new user account.
        /// </summary>
        /// <param name="request">The user profile creation request data.</param>
        /// <returns>Returns the result of the user creation process.</returns>
        /// <response code="200">User successfully created.</response>
        /// <response code="404">If the message is not found.</response>
        /// <response code="500">If there is an internal server error.</response>
        [HttpPost("SignUp")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateUser(UserProfileCreateRequest request)
        {
            try
            {
                var createdUser = await _service.AddUserProfileAsync(request);
                return Ok(new { Success = true, Message = createdUser });
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Verifies the OTP during account verification.
        /// </summary>
        /// <param name="request">The account verification request containing OTP.</param>
        /// <returns>Returns the verification status.</returns>
        /// <response code="200">OTP successfully verified.</response>
        /// <response code="404">OTP invalid or expired.</response>
        /// <response code="500">Server error.</response>
        [HttpPost("AccountVerification")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyOtp(AccountVerification request)
        {
            try
            {
                var result = await _service.VerifyOtpAsync(request);
                return Ok(new { Success = true, Message = result });
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Generates and sends an OTP to the provided email or phone number.
        /// </summary>
        /// <param name="request">The email or phone number for OTP.</param>
        /// <returns>Returns OTP send status.</returns>
        /// <response code="200">OTP sent successfully.</response>
        /// <response code="404">Email or phone not found.</response>
        /// <response code="500">Internal error.</response>
        [HttpPost("LoginVerification")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RequestOtp(EmailRequest request)
        {
            try
            {
                var result = await _service.RequestOtp(request.EmailOrPhone);
                return Ok(new { Success = true, Message = result });
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Verifies the OTP or password during login.
        /// </summary>
        /// <param name="request">The login credentials including name and password or OTP.</param>
        /// <returns>Returns login verification result.</returns>
        /// <response code="200">Login successful.</response>
        /// <response code="404">Invalid credentials.</response>
        /// <response code="500">Internal server error.</response>
        [HttpPost("Login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyOtp(OtpVerificationRequest request)
        {
            try
            {
                var result = await _service.VerifyUserLogin(request.Name, request.PasswordOrOtp);
                return Ok(new { Success = true, Message = result });
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Refreshes the access token using an old refresh token.
        /// </summary>
        /// <param name="oldRefreshToken">The expired or used refresh token.</param>
        /// <returns>Returns a new access token.</returns>
        /// <response code="200">Token refreshed successfully.</response>
        /// <response code="404">Invalid or expired refresh token.</response>
        /// <response code="500">Internal server error.</response>
        [HttpPost("RefreshToken")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RefreshToken(string oldRefreshToken)
        {
            try
            {
                var result = await _service.RefreshTokenAsync(oldRefreshToken);
                return Ok(new { Success = true, Message = result });
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
