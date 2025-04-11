using Microsoft.EntityFrameworkCore;
using QLN.Backend.API.Models;
using QLN.Common.Infrastructure.InputModels;
using QLN.Common.Infrastructure.RepositoryInterface;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace QLN.Common.Infrastructure.Repository
{
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly QatarlivingContext _context;

        public UserProfileRepository(QatarlivingContext context)
        {
            _context = context;
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
    }
}

