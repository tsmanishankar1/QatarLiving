using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Common.Infrastructure.IService.IAuthService
{
    public interface IAuthService
    {
        Task<string> Register(RegisterRequest request, HttpContext context);
        Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, Conflict<ApiResponse<string>>>> SendEmailOtp(string email);
        Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, NotFound<ApiResponse<string>>, BadRequest<ApiResponse<string>>>> VerifyEmailOtp(string email, string otp);
        Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, Conflict<ProblemDetails>>> SendPhoneOtp(string phoneNumber);
        Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, BadRequest<ProblemDetails>>> VerifyPhoneOtp(string phoneNumber, string otp);

        Task<Results<Ok<ApiResponse<string>>, BadRequest<ProblemDetails>, ProblemHttpResult>> ForgotPassword(ForgotPasswordRequest request);
        Task<Results<Ok<ApiResponse<string>>,BadRequest<ProblemDetails>,NotFound<ProblemDetails>,ValidationProblem,ProblemHttpResult>> ResetPassword(ResetPasswordRequest request);
        Task<Results<Ok<LoginResponse>, BadRequest<ProblemDetails>, UnauthorizedHttpResult, ProblemHttpResult, ValidationProblem>> Login(LoginRequest request);
        Task<Results<Ok<ApiResponse<LoginResponse>>, BadRequest<ProblemDetails>, ProblemHttpResult>> Verify2FA(Verify2FARequest request);
        Task<Results<Ok<ApiResponse<RefreshTokenResponse>>, BadRequest<ProblemDetails>, ProblemHttpResult, UnauthorizedHttpResult>> RefreshToken(Guid userId,RefreshTokenRequest request);
        Task<Results< Ok<ApiResponse<string>>, Accepted<ApiResponse<string>>, BadRequest<ProblemDetails>, ProblemHttpResult>> Toggle2FA(TwoFactorToggleRequest request);
        Task<Results<Ok<ApiResponse<object>>,BadRequest<ProblemDetails>,NotFound<ProblemDetails>,ProblemHttpResult>> GetProfile(Guid Id);
        Task<Results< Ok<ApiResponse<string>>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>,ProblemHttpResult>> UpdateProfile(Guid id, UpdateProfileRequest request);
        Task<Results<Ok<ApiResponse<string>>,NotFound<ProblemDetails>,ProblemHttpResult>> Logout(Guid id);
        Task<Results< Ok<ApiResponse<string>>, BadRequest<ProblemDetails>, ProblemHttpResult>> SendTwoFactorOtp(Send2FARequest request);
    }
}
