using System;
using System.Collections.Generic;

namespace QLN.Common.Infrastructure.Models;

public partial class Usertransaction
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string Otp { get; set; } = null!;

    public Guid Createdby { get; set; }

    public Guid? Updatedby { get; set; }

    public DateTime Createdutc { get; set; }

    public DateTime? Updatedutc { get; set; }

    public bool Isactive { get; set; }

    public virtual User CreatedbyNavigation { get; set; } = null!;

    public virtual User? UpdatedbyNavigation { get; set; }
}
