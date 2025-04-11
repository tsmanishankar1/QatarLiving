using System;
using System.Collections.Generic;

namespace QLN.Common.Infrastructure.Models;

public partial class Userprofile
{
    public int Id { get; set; }

    public string Firstname { get; set; } = null!;

    public string Lastname { get; set; } = null!;

    public DateOnly Dateofbirth { get; set; }

    public string Gender { get; set; } = null!;

    public string Mobilenumber { get; set; } = null!;

    public string Emailaddress { get; set; } = null!;

    public string Nationality { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Confirmpassword { get; set; } = null!;

    public string? Languagepreferences { get; set; }

    public string? Location { get; set; }

    public int Createdby { get; set; }

    public int? Updatedby { get; set; }

    public DateTime Createdutc { get; set; }

    public DateTime? Updatedutc { get; set; }

    public virtual ICollection<Otplogin> OtploginCreatedbyNavigations { get; set; } = new List<Otplogin>();

    public virtual ICollection<Otplogin> OtploginUpdatedbyNavigations { get; set; } = new List<Otplogin>();
}
