
using QLN.Common.Infrastructure.InputModels;
using QLN.Common.Infrastructure.Models;
using QLN.Common.Infrastructure.RepositoryInterface;

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
            var message = "Profile Created Successfully";
            var user = new Userprofile
            {
                Firstname = request.Firstname,
                Lastname = request.Lastname,
                Dateofbirth = DateOnly.FromDateTime(request.Dateofbirth),
                Gender = request.Gender,
                Mobilenumber = request.Mobilenumber,
                Emailaddress = request.Emailaddress,
                Nationality = request.Nationality,
                Password = request.Password,
                Confirmpassword = request.Confirmpassword,
                Languagepreferences = request.Languagepreferences,
                Location = request.Location,
                Createdby = request.Createdby,
                Createdutc = DateTime.UtcNow
            };

            _context.Userprofiles.Add(user);
            await _context.SaveChangesAsync();
            return message;
        }
    }
}

