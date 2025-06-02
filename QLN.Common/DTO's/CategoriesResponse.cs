using Microsoft.AspNetCore.Http.Features;
using QLN.Common.Infrastructure.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class BaseCategory
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
    public class Area : BaseCategory;

    public class EventCategory : BaseCategory;

    public class ForumCategory : BaseCategory;

    public class Location : BaseCategory
    {

        [JsonPropertyName("areas")]
        public List<Area> Areas { get; set; }
    }

    public class CategoriesResponse
    {
        [JsonPropertyName("event_categories")]
        public List<EventCategory> EventCategories { get; set; }

        [JsonPropertyName("locations")]
        public List<Location> Locations { get; set; }

        [JsonPropertyName("forum_categories")]
        public List<ForumCategory> ForumCategories { get; set; }
    }
}
