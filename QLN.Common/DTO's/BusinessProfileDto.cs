using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s;

public class BusinessProfileDto : IVerticalDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("permissions")]
    public PermissionDto Permissions { get; set; }
    [JsonPropertyName("verticals")]
    public IEnumerable<string> Verticals { get; set; } = Array.Empty<string>();
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("address")] 
    public string Address { get; set; } = string.Empty;
    [JsonPropertyName("phone")] public string PhoneNumber { get; set; } = string.Empty;
    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
    [JsonIgnore] public DateTime CreatedAt { get; set; }
    [JsonIgnore] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
