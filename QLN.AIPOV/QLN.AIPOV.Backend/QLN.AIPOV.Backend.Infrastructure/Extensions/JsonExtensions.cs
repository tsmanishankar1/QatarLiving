using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace QLN.AIPOV.Backend.Infrastructure.Extensions
{
    public static class JsonExtensions
    {
        public static string GenerateJsonSchema<T>()
        {
            var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true,
                NumberHandling = JsonNumberHandling.Strict,
                Converters = { new JsonStringEnumConverter() },
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                RespectNullableAnnotations = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
            };

            var exporterOptions = new JsonSchemaExporterOptions
            {
                TreatNullObliviousAsNonNullable = true
            };

            var schemaNode = serializerOptions.GetJsonSchemaAsNode(typeof(T), exporterOptions);
            return schemaNode.ToJsonString(serializerOptions);
        }
    }
}
