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


        public class LocationEventDto
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Latitude { get; set; }
            public string Longitude { get; set; }

            public List<string>  Areas { get; set; }
        }

        public class LocationListResponseDto
        {
            public List<LocationEventDto> Locations { get; set; }
        }


    }
}
