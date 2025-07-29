using System.ComponentModel.DataAnnotations;

namespace QLN.ContentBO.WebUI.Models
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

}