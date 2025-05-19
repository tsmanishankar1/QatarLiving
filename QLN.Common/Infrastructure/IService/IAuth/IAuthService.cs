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
        Task<Results<Ok<string>, ProblemHttpResult, Conflict<ProblemDetails>>> SendEmailOtp(string email);
        Task<Results<Ok<string>, ProblemHttpResult, NotFound<string>, BadRequest<string>>> VerifyEmailOtp(string email, string otp);
        Task<Results<Ok<string>, ProblemHttpResult, Conflict<ProblemDetails>>> SendPhoneOtp(string phoneNumber);
        Task<Results<Ok<string>, ProblemHttpResult, BadRequest<ProblemDetails>>> VerifyPhoneOtp(string phoneNumber, string otp);

        Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>> ForgotPassword(ForgotPasswordRequest request);
        Task<Results<Ok<string>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, ValidationProblem, ProblemHttpResult>> ResetPassword(ResetPasswordRequest request);
        Task<Results<Ok<LoginResponse>, BadRequest<ProblemDetails>, UnauthorizedHttpResult, ProblemHttpResult, ValidationProblem>> Login(LoginRequest request);
        Task<Results<Ok<LoginResponse>, BadRequest<ProblemDetails>, ProblemHttpResult>> Verify2FA(Verify2FARequest request);
        Task<Results<Ok<RefreshTokenResponse>, BadRequest<ProblemDetails>, ProblemHttpResult, UnauthorizedHttpResult>> RefreshToken(Guid userId,RefreshTokenRequest request);
        Task<Results<Ok<string>, Accepted<string>, BadRequest<ProblemDetails>, ProblemHttpResult>> Toggle2FA(TwoFactorToggleRequest request);
        Task<Results<Ok<object>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, ProblemHttpResult>> GetProfile(Guid Id);
        Task<Results<Ok<string>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, ProblemHttpResult>> UpdateProfile(Guid id, UpdateProfileRequest request);
        Task<Results<Ok<string>, NotFound<ProblemDetails>, ProblemHttpResult>> Logout(Guid id);
        Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>> SendTwoFactorOtp(Send2FARequest request);
    }
}
