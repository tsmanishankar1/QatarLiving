using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Company
{
    public enum VerifiedStatus
    {
        Pending = 1,
        Approved = 2,
        NeedChanges = 3,
        Rejected = 4,
        Removed = 5,
        OnHold = 6,
        NotVerified = 7,
        Verified = 8
    }
}
