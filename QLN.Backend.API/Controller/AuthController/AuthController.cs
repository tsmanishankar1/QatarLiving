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
        /// <remarks>
        /// This endpoint registers a new user into the system.
        /// Use this to create user profile records.
        /// </remarks>
        [HttpPost("SignUp")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateUser(UserProfileCreateRequest request)
        {
            try
            {
                var createdUser = await _service.AddUserProfileAsync(request);
                var response = new
                {
                    Success = true,
                    Message = createdUser
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
           
        }
        /// <summary>
        /// Generates an OTP and sends it to the provided email address.
        /// </summary>
        /// <param name="request">Contains the email to which the OTP should be sent.</param>
        /// <returns>Returns the result of the OTP generation process.</returns>
        /// <response code="200">OTP successfully generated and sent.</response>
        /// <response code="404">If the email is not found.</response>
        /// <response code="500">If there is an internal server error.</response>
        /// <remarks>
        /// This endpoint sends an OTP to the given email.
        /// It is typically used for email verification.
        /// </remarks>
        [HttpPost("GenerateEmailOtp")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RequestOtp(EmailRequest request)
        {
            try
            {
                var requestotp = await _service.RequestOtp(request.Email);
                var response = new
                {
                    Success = true,
                    Message = requestotp
                };
                return Ok(response);
            }
            catch (Exception ex )
            {
                return NotFound(ex.Message);
            }
           
        }
        /// <summary>
        /// Verifies the OTP sent to the user's email address.
        /// </summary>
        /// <param name="request">Contains the OTP to verify.</param>
        /// <returns>Returns the result of the OTP verification.</returns>
        /// <response code="200">OTP successfully verified.</response>
        /// <response code="404">If the OTP is invalid or expired.</response>
        /// <response code="500">If there is an internal server error.</response>
        /// <remarks>
        /// This endpoint is used to verify the OTP token entered by the user.
        /// A valid token confirms identity verification.
        /// </remarks>
        [HttpPost("VerifyOtpWithToken")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyOtp(OtpVerificationRequest request)
        {
            try
            {
                var VerifyOtpWithToken = await _service.VerifyOtpWithToken(request.Email, request.Otp);
                var response = new
                {
                    Success = true,
                    Message = VerifyOtpWithToken
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
            
        }
    }
}
