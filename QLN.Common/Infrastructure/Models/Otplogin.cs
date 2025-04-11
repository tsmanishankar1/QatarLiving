using System;
using System.Collections.Generic;

namespace QLN.Common.Infrastructure.Models;

public partial class Otplogin
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Otp { get; set; } = null!;

    public int Createdby { get; set; }

    public int? Updatedby { get; set; }

    public DateTime Createdutc { get; set; }

    public DateTime? Updatedutc { get; set; }

    public virtual Userprofile CreatedbyNavigation { get; set; } = null!;

    public virtual Userprofile? UpdatedbyNavigation { get; set; }
}
