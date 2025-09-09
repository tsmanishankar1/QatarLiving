using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public enum AdStatus
    {
        Draft = 0,
        PendingApproval = 1,
        Approved = 2,
        Published = 3,
        Unpublished = 4,        
        Rejected = 5,
        Expired = 6,
        NeedsModification = 7,
        Hold = 8,
        Onhold = 9
    }
}
