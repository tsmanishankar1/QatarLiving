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
            { "awareness", "Awareness" },
            { "classes_and_workshops", "Classes & Workshops" },
            { "conferences", "Conferences" },
            { "entertainment", "Entertainment" },
            { "exhibition", "Exhibition" },
            { "festivals", "Festivals" },
            { "fundraisers", "Fundraisers" },
            { "lifestyle", "Lifestyle" },
            { "meetings_and_networking", "Meetings & Networking" },
            { "music", "Music" },
            { "performing_arts", "Performing Arts" },
            { "social_events", "Social Events" },
            { "sports", "Sports" },
            { "training", "Training" },
            { "others", "Others" }
        };
    }
}
