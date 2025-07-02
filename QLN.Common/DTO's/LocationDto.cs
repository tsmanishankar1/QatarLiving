using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class LocationDto
    {
        public class LocationZoneDto
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
        public class LocationZoneListDto
        {
            public List<LocationZoneDto> Zones { get; set; } = new();
        }

        public class AddressResponseDto
        {
            public List<string> Coordinates { get; set; }
        }

        public class LocationNameDto
        {
            public string Name { get; set; } = string.Empty;
        }
        public class LocationNameDtoList
        {
            public List<LocationNameDto> Locations { get; set; }
        }


        public class AreaDto
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("latitude")]
            public string Latitude { get; set; }
            [JsonProperty("longitude")]
            public string Longitude { get; set; }

        }

        public class LocationEventDto
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("latitude")]
            public string Latitude { get; set; }
            [JsonProperty("longitude")]
            public string Longitude { get; set; }
            [JsonProperty("areas")]
            public List<AreaDto> Areas { get; set; }
        }

        public class LocationListResponseDto
        {
            [JsonProperty("locations")]
            public List<LocationEventDto> Locations { get; set; }
        }

    }
}
