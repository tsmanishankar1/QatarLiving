using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class RegisterRequest
    {
        public string Username { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string Lastname { get; set; } = null!;
        public DateOnly Dateofbirth { get; set; }
        public string Gender { get; set; } = null!;
        public string MobileOperator { get; set; } = null;
        public string Mobilenumber { get; set; } = null!;
        public string Emailaddress { get; set; } = null!;
        public string Nationality { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? Languagepreferences { get; set; }
        public string? Location { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string UsernameOrEmailOrPhone { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string MobileNumber { get; set; } = null!;
        public string Location { get; set; } = null!;
    }

}
