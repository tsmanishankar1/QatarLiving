using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.Infrastructure.InputModels;
using QLN.Common.Infrastructure.ServiceInterface;

namespace QLN.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   
    public class AuthController : ControllerBase
    {
        private readonly IOtpAuthService _authService;

        public AuthController(IOtpAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("GenerateEmailOtp")]
        public async Task<IActionResult> RequestOtp(EmailRequest request)
        {
            try
            {
                var requestotp = await _authService.RequestOtp(request.Email);
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
                var VerifyOtpWithToken = await _authService.VerifyOtpWithToken(request.Otp);
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
