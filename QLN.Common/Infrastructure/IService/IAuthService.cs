using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService
{
    public interface IAuthService
    {
        Task<Results<Ok<ApiResponse<string>>, ValidationProblem>> RegisterAsync(RegisterRequest request, HttpContext context);

        Task<Ok<ApiResponse<string>>> SendEmailOtpAsync(string email);
        
        Task<Results<Ok<ApiResponse<string>>, BadRequest<string>>> VerifyEmailOtpAsync(string email, string otp);

        Task<Ok<ApiResponse<string>>> SendPhoneOtpAsync(string phoneNumber);

        Task<Results<Ok<ApiResponse<string>>, BadRequest<string>>> VerifyPhoneOtpAsync(string phoneNumber, string otp);
      
        Task<Ok<ApiResponse<string>>> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<Results<Ok<ApiResponse<string>>, ValidationProblem>> ResetPasswordAsync(ResetPasswordRequest request);
        Task<Results<Ok<ApiResponse<LoginResponse>>, UnauthorizedHttpResult, ValidationProblem>> LoginAsync(LoginRequest request);
        Task<Results<Ok<ApiResponse<LoginResponse>>, ValidationProblem, NotFound>> Verify2FAAsync(Verify2FARequest request);
        Task<Results<Ok<ApiResponse<RefreshTokenResponse>>, UnauthorizedHttpResult>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<IResult> Toggle2FAAsync(TwoFactorToggleRequest request);
        Task<IResult> GetProfileAsync(string identity);
        Task<IResult> UpdateProfileAsync(UpdateProfileRequest request);        
    }
}
