using System.Text.Json.Serialization;

namespace QLN.DataMigration.Models
{
    public class DrupalMake
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("models")]
        public List<DrupalModel> Models { get; set; }
    }

    public class DrupalModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class DrupalItemsMobileDevices
    {
        [JsonPropertyName("makes")]
        public List<DrupalMake> Makes { get; set; }
    }
}
