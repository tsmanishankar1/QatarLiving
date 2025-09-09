using System.Text.Json.Serialization;

public class CardDetails
{
    [JsonPropertyName("card_type")]
    public string CardType { get; set; } = string.Empty;

    [JsonPropertyName("last_4_digits")]
    public string Last4Digits { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
