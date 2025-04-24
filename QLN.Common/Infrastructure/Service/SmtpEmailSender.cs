using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Net;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.IService;
namespace QLN.Common.Infrastructure.Service
{
    public class SmtpEmailSender : IExtendedEmailSender<ApplicationUser>
    {
        private readonly IConfiguration _config;
        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        {
            var subject = "Confirm your email - Qatar Living";
            var body = $"Hi {user.UserName},<br/><br/>" +
                       $"Please confirm your account by clicking the link below:<br/>" +
                       $"<a href='{confirmationLink}'>Confirm Email</a><br/><br/>" +
                       $"Thanks,<br/>Qatar Living Team";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        {
            var subject = "Password Reset Code";
            var body = $"Hi {user.UserName},<br/>Your password reset code is: <b>{resetCode}</b>";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        {
            var subject = "Reset your password";
            var body = $"Hi {user.UserName},<br/>Reset your password using this link: <a href='{resetLink}'>Reset Password</a>";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendTwoFactorCode(ApplicationUser user, string email, string code)
        {
            var subject = "Your OTP for login - Qatar Living";
            var body = $@" 
                Hi {user.UserName},<br/><br/>
                Your One-Time Password (OTP) for login is: <b>{code}</b><br/><br/>
                This OTP will expire shortly. Do not share this with anyone.<br/><br/>
                Thanks,<br/>Qatar Living Team";

            await SendEmailAsync(email, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var smtpConfig = _config.GetSection("Smtp");

            var client = new SmtpClient(smtpConfig["Host"], int.Parse(smtpConfig["Port"]))
            {
                Credentials = new NetworkCredential(smtpConfig["Username"], smtpConfig["Password"]),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(smtpConfig["email"], smtpConfig["DisplayName"]),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
        }              
    }
}
