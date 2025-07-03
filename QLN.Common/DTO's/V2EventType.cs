using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public enum V2EventType
    {
        FreeAcess = 1,
        OpenRegistrations = 2,
        FeePrice = 3
    }
    public enum V2EventTimeType
    {
        GeneralTime = 1,
        PerDayTime = 2
    }
    public enum EventStatus
    {
        Published = 1,
        UnPublished = 2,
        Expired = 3
    }
}