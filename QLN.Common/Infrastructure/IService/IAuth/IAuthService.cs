using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Common.Infrastructure.IService.IAuthService
{
    public interface IAuthService
    {
        Task<Results<Ok<ApiResponse<string>>, BadRequest<ApiResponse<string>>, ValidationProblem, NotFound<ApiResponse<string>>, Conflict<ApiResponse<string>>, ProblemHttpResult>> Register(RegisterRequest request, HttpContext context);
        Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, BadRequest<ApiResponse<string>>>> SendEmailOtp(string email);
        Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, BadRequest<ApiResponse<string>>>> VerifyEmailOtp(string email, string otp);
        Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, BadRequest<ApiResponse<string>>>> SendPhoneOtp(string phoneNumber);
        Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, BadRequest<ApiResponse<string>>>> VerifyPhoneOtp(string phoneNumber, string otp);
        Task<Results<Ok<ApiResponse<string>>, ProblemHttpResult, BadRequest<ApiResponse<string>>>> ForgotPassword(ForgotPasswordRequest request);
        Task<Results<Ok<ApiResponse<string>>, BadRequest<ApiResponse<string>>, NotFound<ApiResponse<string>>, ValidationProblem, ProblemHttpResult>> ResetPassword(ResetPasswordRequest request);
        Task<Results<Ok<ApiResponse<LoginResponse>>, BadRequest<ApiResponse<string>>, UnauthorizedHttpResult, ProblemHttpResult, ValidationProblem>> Login(LoginRequest request);
        Task<Results<Ok<ApiResponse<LoginResponse>>, BadRequest<ApiResponse<string>>, ProblemHttpResult, ValidationProblem>> Verify2FA(Verify2FARequest request);
        Task<Results<Ok<ApiResponse<RefreshTokenResponse>>, BadRequest<ApiResponse<string>>, ProblemHttpResult, UnauthorizedHttpResult>> RefreshToken(RefreshTokenRequest request);
        Task<IResult> Toggle2FA(TwoFactorToggleRequest request);
        Task<IResult> GetProfile(Guid Id);
        Task<IResult> UpdateProfile(Guid id, UpdateProfileRequest request);
        Task<IResult> Logout(Guid id);
        Task<ApiResponse<string>> SendTwoFactorOtp(Send2FARequest request);
    }
}
