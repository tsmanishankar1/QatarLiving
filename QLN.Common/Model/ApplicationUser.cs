using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Model
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string Username { get; set; } = null!;
        public string? Mobileoperator { get; set; }
        public string Firstname { get; set; } = null!;
        public string Lastname { get; set; } = null!;
        public DateOnly Dateofbirth { get; set; }
        public string Gender { get; set; } = null!;
        public string Mobilenumber { get; set; } = null!;
        public string Emailaddress { get; set; } = null!;
        public string Nationality { get; set; } = null!;
        public string? Languagepreferences { get; set; }
        public string? Location { get; set; }
        public bool Isactive { get; set; } = true;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
    }
}
