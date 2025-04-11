using System;
using System.Collections.Generic;

namespace QLN.Backend.API.Models;

public partial class User
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

    public bool Isactive { get; set; }

    public virtual ICollection<Usertransaction> UsertransactionCreatedbyNavigations { get; set; } = new List<Usertransaction>();

    public virtual ICollection<Usertransaction> UsertransactionUpdatedbyNavigations { get; set; } = new List<Usertransaction>();
}
