using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class ApplicationUser : IdentityUser<Guid>
    {        
        public string? Mobileoperator { get; set; }
        public string Firstname { get; set; } = null!;
        public string Lastname { get; set; } = null!;
        public DateOnly Dateofbirth { get; set; }
        public string Gender { get; set; } = null!;
        public string Nationality { get; set; } = null!;
        public string? Languagepreferences { get; set; }
        public string? Location { get; set; }
        public bool IsCompany { get; set; } = false;
        public bool Isactive { get; set; } = true;
    }
}
