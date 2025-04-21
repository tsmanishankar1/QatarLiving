using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Net;
using QLN.Common.Infrastructure.Model;
namespace QLN.Common.Infrastructure.Service
{
    public class SmtpEmailSender : IEmailSender<ApplicationUser>
    {
        private readonly IConfiguration _config;
        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        {
            var subject = "Confirm your email - Qatar Living";
            var body = $"Hi {user.Username},<br/><br/>" +
                       $"Please confirm your account by clicking the link below:<br/>" +
                       $"<a href='{confirmationLink}'>Confirm Email</a><br/><br/>" +
                       $"Thanks,<br/>Qatar Living Team";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        {
            var subject = "Password Reset Code";
            var body = $"Hi {user.Username},<br/>Your password reset code is: <b>{resetCode}</b>";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        {
            var subject = "Reset your password";
            var body = $"Hi {user.Username},<br/>Reset your password using this link: <a href='{resetLink}'>Reset Password</a>";

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
                From = new MailAddress(smtpConfig["From"], smtpConfig["DisplayName"]),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
        }       
    }
}
