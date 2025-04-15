using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
using QLN.Common.Infrastructure.Models;
using System.Security.Cryptography;

namespace QLN.Common.Infrastructure.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly QatarlivingDevContext _context;
        private readonly IConfiguration _config;
        private readonly IEventlogger _eventLogger;

        public AuthRepository(QatarlivingDevContext context, IConfiguration config, IEventlogger eventLogger)
        {
            _context = context;
            _config = config;
            _eventLogger = eventLogger;
        }
        public async Task<string> AddUserProfileAsync(UserProfileCreateRequest request)
        {
            try
            {
                if (!new EmailAddressAttribute().IsValid(request.Emailaddress))
                {
                    throw new ArgumentException("Invalid email address format.");
                }

                if (!Regex.IsMatch(request.Mobilenumber, @"^\+?[0-9]{7,15}$"))
                {
                    throw new ArgumentException("Invalid mobile number format. It should be between 7 to 15 digits.");
                }

                bool isExistingUser = await _context.Users
                    .AnyAsync(u => u.Emailaddress == request.Emailaddress || u.Mobilenumber == request.Mobilenumber);

                if (isExistingUser)
                {
                    throw new ArgumentException("A user with this email or mobile number already exists.");
                }

                var hashedPassword = HashPassword(request.Password);
                var user = new User
                {
                    Firstname = request.Firstname,
                    Lastname = request.Lastname,
                    Dateofbirth = request.Dateofbirth,
                    Gender = request.Gender,
                    Mobilenumber = request.Mobilenumber,
                    Emailaddress = request.Emailaddress,
                    Nationality = request.Nationality,
                    Password = hashedPassword,
                    Languagepreferences = request.Languagepreferences,
                    Location = request.Location,
                    Isactive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                string otp = new Random().Next(100000, 999999).ToString();

                var otpEntry = new Usertransaction
                {
                    Email = request.Emailaddress,
                    Otp = otp,
                    Isactive = true,
                    Createdby = user.Id,
                    Createdutc = DateTime.UtcNow
                };

                _context.Usertransactions.Add(otpEntry);
                await _context.SaveChangesAsync();
                await SendEmailAsync(request.Emailaddress, "Your OTP Code", $"Your OTP is: {otp}");

                return "Profile created successfully. OTP has been sent to your email.";
            }
            catch (Exception ex)
            {
                _eventLogger.LogException(ex);
                throw;
            }
        }
        public async Task<string> VerifyOtpAsync(AccountVerification request)
        {
            try
            {
                var otpRecord = await _context.Usertransactions
                    .Where(x => x.Email == request.Email && x.Otp == request.Otp && x.Isactive && x.Createdutc.AddMinutes(10) >= DateTime.UtcNow)
                    .OrderByDescending(x => x.Createdutc)
                    .FirstOrDefaultAsync();

                if (otpRecord == null)
                    throw new Exception("Invalid or expired OTP.");

                otpRecord.Isactive = false;
                _context.Usertransactions.Update(otpRecord);
                await _context.SaveChangesAsync();

                return "Signed in successfully";
            }
            catch (Exception ex)
            {
                _eventLogger.LogException(ex);
                throw;
            }
        }
        public async Task<string> RequestOtp(string name)
        {
            try
            {
                var user = await _context.Users 
                    .FirstOrDefaultAsync(u =>u.Emailaddress.ToLower().Trim() == name.ToLower().Trim() || u.Mobilenumber.Trim() == name.Trim());

                if (user == null)
                    throw new Exception("Email or phone number not registered.");

                string otp = new Random().Next(100000, 999999).ToString();

                var otpEntry = new Usertransaction
                {
                    Email = user.Emailaddress,
                    Otp = otp,
                    Isactive = true,
                    Createdby = user.Id,
                    Createdutc = DateTime.UtcNow
                };

                _context.Usertransactions.Add(otpEntry);
                await _context.SaveChangesAsync();

                await SendEmailAsync(name, "Your OTP Code", $"Your OTP is: {otp}");
                return "OTP sent to your email.";
            }
            catch (Exception ex)
            {
                _eventLogger.LogException(ex);
                throw; 
            }
        }
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
        }
        public async Task<string> RefreshTokenAsync(string oldRefreshToken)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.RefreshToken == oldRefreshToken);

                if (user == null)
                    throw new Exception("Invalid refresh token");

                if (user.RefreshTokenExpiry == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                    throw new Exception("Refresh token has expired");

                var newRefreshToken = GenerateRefreshToken();
                var newExpiry = DateTime.UtcNow.AddDays(1); 

                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiry = newExpiry;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return newRefreshToken;
            }
            catch (Exception ex)
            {
                _eventLogger.LogException(ex);
                throw;
            }
        }

        public async Task<LoginResponse> VerifyUserLogin(string name, string passwordOrOtp)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.Emailaddress == name ||
                        u.Mobilenumber == name ||
                        u.Firstname == name);

                if (user == null)
                    throw new Exception("User not found");

                if (Regex.IsMatch(passwordOrOtp, @"^\d{6}$"))
                {
                    var otpRecord = await _context.Usertransactions
                        .Where(x => x.Email == user.Emailaddress && x.Otp == passwordOrOtp && x.Createdutc.AddMinutes(10) >= DateTime.UtcNow)
                        .OrderByDescending(x => x.Createdutc)
                        .FirstOrDefaultAsync();

                    if (otpRecord == null)
                        throw new Exception("Invalid or expired OTP");
                }
                else
                {
                    var hashedInputPassword = HashPassword(passwordOrOtp);
                    if (user.Password != hashedInputPassword)
                        throw new Exception("Invalid password");
                }
                var jwtToken = GenerateJwtToken(user.Emailaddress);
                var refreshToken = GenerateRefreshToken();
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(1); 
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = refreshTokenExpiry;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return new LoginResponse
                {
                    JwtToken = jwtToken,
                    RefreshToken = refreshToken,
                };
            }
            catch (Exception ex)
            {
                _eventLogger.LogException(ex);
                throw;
            }
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

