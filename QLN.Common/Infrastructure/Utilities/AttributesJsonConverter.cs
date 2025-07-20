using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using Newtonsoft.Json.Linq;

public class AttributesJsonConverter : JsonConverter<object>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsClass && typeToConvert.GetProperty("AttributesJson") != null;
    }

    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var defaultOptions = new JsonSerializerOptions(options);
        defaultOptions.Converters.Remove(this);
        return JsonSerializer.Deserialize(ref reader, typeToConvert, defaultOptions)
               ?? Activator.CreateInstance(typeToConvert)!;
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        var type = value.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var propertyValue = property.GetValue(value);

            if (property.Name == "AttributesJson")
            {
                var attributesJsonString = propertyValue?.ToString();
                if (!string.IsNullOrEmpty(attributesJsonString))
                {
                    try
                    {
                        var attributesObject = JObject.Parse(attributesJsonString);
                        writer.WritePropertyName("attributes");
                        writer.WriteRawValue(attributesObject.ToString(Newtonsoft.Json.Formatting.None));
                    }
                    catch
                    {
                        writer.WritePropertyName("attributesText");
                        writer.WriteStringValue(attributesJsonString);
                    }
                }
                continue;
            }

            if (propertyValue == null ||
                (property.PropertyType == typeof(DateTime) && (DateTime)propertyValue == DateTime.MinValue) ||
                (property.PropertyType == typeof(DateTime?) && propertyValue.Equals(DateTime.MinValue)))
            {
                continue;
            }

            var camelCaseName = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            writer.WritePropertyName(camelCaseName);
            JsonSerializer.Serialize(writer, propertyValue, propertyValue.GetType(), options);
        }

        writer.WriteEndObject();
    }
}
