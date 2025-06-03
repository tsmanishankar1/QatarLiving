using Microsoft.AspNetCore.Identity;

namespace QLN.Common.Infrastructure.IService.IEmailService
{
    public interface IExtendedEmailSender<TUser> : IEmailSender<TUser>
       where TUser : class
    {
        Task SendTwoFactorCode(TUser user, string email, string code);
        Task SendOtpEmailAsync(string email, string otp);
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }
}
