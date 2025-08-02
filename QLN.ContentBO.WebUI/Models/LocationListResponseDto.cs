using System.ComponentModel.DataAnnotations;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json.Serialization;
namespace QLN.ContentBO.WebUI.Models
{
    public class LocationListResponseDto
    {
        [JsonPropertyName($"locations")]
        public List<LocationEventDto> Locations { get; set; }
    }
    public class AreaDto
    {
        [JsonPropertyName($"id")]
        public string Id { get; set; }
        [JsonPropertyName($"name")]
        public string Name { get; set; }
        [JsonPropertyName($"latitude")]
        public string Latitude { get; set; }
        [JsonPropertyName($"longitude")]
        public string Longitude { get; set; }

    }

    public class LocationEventDto
    {
        [JsonPropertyName($"id")]
        public string Id { get; set; }
        [JsonPropertyName($"name")]
        public string Name { get; set; }
        [JsonPropertyName($"latitude")]
        public string Latitude { get; set; }
        [JsonPropertyName($"longitude")]
        public string Longitude { get; set; }
        [JsonPropertyName($"areas")]
        public List<AreaDto> Areas { get; set; }
    }
        
          public class LocationZoneDto
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
        public class LocationZoneListDto
        {
            public List<LocationZoneDto> Zones { get; set; } = new();
        }

}