using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QLN.Backend.API.Models;
using QLN.Common.Infrastructure.InputModels;
using QLN.Common.Infrastructure.RepositoryInterface;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;



namespace QLN.Common.Infrastructure.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly QatarlivingDevContext _context;
        private readonly IConfiguration _config;

        public AuthRepository(QatarlivingDevContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        public async Task<string> AddUserProfileAsync(UserProfileCreateRequest request)
        {
            if (!new EmailAddressAttribute().IsValid(request.Emailaddress))
            {
                throw new ArgumentException("Invalid email address format.");
            }
            if (!Regex.IsMatch(request.Mobilenumber, @"^\+?[0-9]{7,15}$"))
            {
                throw new ArgumentException("Invalid mobile number format. It should be between 7 to 15 digits.");
            }
            if (request.Password != request.Confirmpassword)
            {
                throw new ArgumentException("Passwords do not match.");
            }
            bool isExistingUser = await _context.Users
            .AnyAsync(u => u.Emailaddress == request.Emailaddress || u.Mobilenumber == request.Mobilenumber);
            if (isExistingUser)
            {
                throw new ArgumentException("A user with this email or mobile number already exists.");
            }
            var message = "Profile Created Successfully";
            var user = new User
            {
                Firstname = request.Firstname,
                Lastname = request.Lastname,
                Dateofbirth = request.Dateofbirth,
                Gender = request.Gender,
                Mobilenumber = request.Mobilenumber,
                Emailaddress = request.Emailaddress,
                Nationality = request.Nationality,
                Password = request.Password,
                Confirmpassword = request.Confirmpassword,
                Languagepreferences = request.Languagepreferences,
                Location = request.Location,
                Isactive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return message;
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

