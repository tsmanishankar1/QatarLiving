using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public static class EventCategory
    {
        public static readonly Dictionary<string, string> Categories = new()
        {
            { "music", "Music" },
            { "performing_arts", "Performing Arts" },
            { "classes_and_workshops", "Classes & Workshops" },
            { "festivals", "Festivals" },
            { "exhibition", "Exhibition" },
            { "lifestyle", "Lifestyle" },
            { "social_events", "Social Events" },
            { "entertainment", "Entertainment" }
        };
    }
}
