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

        [HttpPost("request-otp")]
        public async Task<IActionResult> RequestOtp( EmailRequest request)
        {
            var result = await _authService.RequestOtpAsync(request.Email);
            return Ok(new { message = result });
        }


        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(OtpVerificationRequest request)
        {
            var token = await _authService.VerifyOtpAsync(request.Otp);
            return Ok(new { token });
        }




    }



}
