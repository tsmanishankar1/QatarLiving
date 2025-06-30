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
    public class V2Slot
    {
        public EventSlot Id { get; set; }
        public string Name { get; set; }
    }
    public enum EventSlot
    {
        Slot1 = 1,
        Slot2 = 2,
        Slot3 = 3,
        Slot4 = 4,
        Slot5 = 5,
        Slot6 = 6,
        Published = 7,
        UnPublished = 8,
        Expired = 9
    }
}
