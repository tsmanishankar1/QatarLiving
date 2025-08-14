using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Implio
{
    public class ImplioError
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("params")]
        public ImplioParams Params { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("dataPath")]
        public string DataPath { get; set; }

        [JsonPropertyName("schemaPath")]
        public string SchemaPath { get; set; }
    }

}
