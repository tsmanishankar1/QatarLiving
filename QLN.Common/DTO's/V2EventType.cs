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
    public class PerDayTime
    {
        public DayOfWeek Day { get; set; }   
        public TimeOnly StartTime { get; set; } 
        public TimeOnly EndTime { get; set; }  
    }
    public class V2Category
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public List<V2EventSubCategory> SubCategories { get; set; }
    }
    public class V2EventSubCategory
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
    }
    public class V2Slot
    {
        public Slot Id { get; set; }
        public string Name { get; set; }
    }
    public enum Slot
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
    public enum V2EventCategory
    {
        Awareness = 1,
        ClassesAndWorkshops = 2,
        Conferences = 3,
        Entertainment = 4,
        Exhibition = 5,
        Festivals = 6,
        Fundraisers = 7,
        Lifestyle = 8,
        MeetingsAndNetworking = 9,
        Music = 10,
        PerformingArts = 11,
        SocialEvents = 12,
        Sports = 13,
        Training = 14,
        Others = 15
    }
}
