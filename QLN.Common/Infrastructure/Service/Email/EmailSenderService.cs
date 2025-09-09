using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Net;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.IService.IEmailService;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
namespace QLN.Common.Infrastructure.Service.SmtpService
{
    public class EmailSenderService : IExtendedEmailSender<ApplicationUser>
    {
        private readonly IConfiguration _config;
        public EmailSenderService(IConfiguration config)
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
            var body = $"Dear {user.UserName},<br/><br/>You requested to reset your password. Please click the link below to reset your password: <a href='{resetLink}'>Reset your password</a><br/>" +
                $"This link will expire in 30 minutes<br/>" +
                $"If you did not request this, please ignore this email<br/><br/>" +
                $"Best regards,<br/>" +
                $"Qatar Living";


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

        public async Task SendOtpEmailAsync(string email, string otp)
        {
            var subject = "Your Email OTP - Qatar Living";
            var body = $"Hi,<br/><br/>Your One-Time Password (OTP) is: <b>{otp}</b><br/><br/>Thanks,<br/>Qatar Living Team";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
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
        public async Task SendEmail(string toEmail, string subject, string htmlBody)
        {
            var smtpConfig = _config.GetSection("Smtp");

            string host = smtpConfig["Host"];
            string portStr = smtpConfig["Port"];
            string username = smtpConfig["Username"];
            string password = smtpConfig["Password"];
            string fromEmail = smtpConfig["email"];
            string displayName = smtpConfig["DisplayName"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(portStr) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new InvalidOperationException("SMTP configuration is missing required values.");
            }

            if (!int.TryParse(portStr, out int port))
            {
                throw new InvalidOperationException("SMTP port is not a valid number.");
            }

            var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail, displayName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
        }
        public string GetApprovalEmailTemplate(string companyName)
        {
         return $@"Hi {companyName},<br/><br/>
        Your company profile has been approved by the Qatar Living team.<br/>
        You can now access features exclusive to verified companies.<br/><br/>
        Regards,<br/>Qatar Living Team";
        }
    }
}
