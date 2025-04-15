using Microsoft.AspNetCore.Mvc;
using QLN.Common.Infrastructure.InputModels;
using QLN.Common.Infrastructure.ServiceInterface;

namespace QLN.Backend.API.Controller.AuthController
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;

        public AuthController(IAuthService service)
        {
            _service = service;
        }

        [HttpPost("SignUp")]
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
            catch (MessageNotFoundException ex)
            {
                return ErrorClass.NotFoundResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorClass.ErrorResponse(ex.Message);
            }
        }
        [HttpPost("GenerateEmailOtp")]
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
            catch (MessageNotFoundException ex)
            {
                return ErrorClass.NotFoundResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorClass.ErrorResponse(ex.Message);
            }
        }
        [HttpPost("VerifyOtpWithToken")]
        public async Task<IActionResult> VerifyOtp(OtpVerificationRequest request)
        {
            try
            {
                var VerifyOtpWithToken = await _service.VerifyOtpWithToken(request.Otp);
                var response = new
                {
                    Success = true,
                    Message = VerifyOtpWithToken
                };
                return Ok(response);
            }
            catch (MessageNotFoundException ex)
            {
                return ErrorClass.NotFoundResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorClass.ErrorResponse(ex.Message);
            }
        }
    }
}
