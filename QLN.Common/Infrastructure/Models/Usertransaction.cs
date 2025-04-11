using System;
using System.Collections.Generic;

namespace QLN.Backend.API.Models;

public partial class Usertransaction
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Otp { get; set; } = null!;

    public int Createdby { get; set; }

    public int? Updatedby { get; set; }

    public DateTime Createdutc { get; set; }

    public DateTime? Updatedutc { get; set; }

    public bool Isactive { get; set; }

    public virtual User CreatedbyNavigation { get; set; } = null!;

    public virtual User? UpdatedbyNavigation { get; set; }
}
