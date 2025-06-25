using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s;

public class CreateBusinessProfileDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;
    [JsonPropertyName("phone")] public string PhoneNumber { get; set; } = string.Empty;
    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
    [JsonIgnore] public DateTime CreatedAt { get; set; }
}
