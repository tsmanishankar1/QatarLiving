using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using QLN.Common.Infrastructure.RepositoryInterface;
using QLN.Backend.API.Models;

namespace QLN.Common.Infrastructure.Repository
{
    public class OtpRepository : IOtpRepository
    {
        private readonly QatarlivingContext _context;
        private readonly IConfiguration _config;

        public OtpRepository(QatarlivingContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        public async Task<string> RequestOtp(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Emailaddress == email);
            if (user == null)
                throw new Exception("Email not registered.");

            string otp = new Random().Next(100000, 999999).ToString();

            var otpEntry = new Usertransaction
            {
                Email = email,
                Otp = otp,
                Isactive = true,
                Createdby = user.Id,
                Createdutc = DateTime.UtcNow
            };
            _context.Usertransactions.Add(otpEntry);
            await _context.SaveChangesAsync();
            await SendEmailAsync(email, "Your OTP Code", $"Your OTP is: {otp}");
            return "OTP sent to your email.";
        }

        public async Task<string> VerifyOtpWithToken(string otp)
        {
            var otpRecord = await _context.Usertransactions
                .Where(x => x.Otp == otp && x.Createdutc.AddMinutes(10) >= DateTime.UtcNow)
                .OrderByDescending(x => x.Createdutc)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
                throw new Exception("Invalid or expired OTP");

            return GenerateJwtToken(otpRecord.Email);
        }
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            using var client = new SmtpClient(_config["Smtp:Host"], int.Parse(_config["Smtp:Port"]!))
            {
                Credentials = new NetworkCredential(_config["Smtp:Username"], _config["Smtp:Password"]),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_config["Smtp:From"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            mailMessage.To.Add(toEmail);
            await client.SendMailAsync(mailMessage);
        }
        private string GenerateJwtToken(string email)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.Email, email)
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
