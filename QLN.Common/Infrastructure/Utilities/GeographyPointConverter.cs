using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Spatial;

public class GeographyPointConverter : JsonConverter<GeographyPoint>
{
    public override GeographyPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");

        double? latitude = null;
        double? longitude = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString().ToLowerInvariant();
                reader.Read();

                switch (propertyName)
                {
                    case "latitude":
                    case "lat":
                        latitude = reader.GetDouble();
                        break;
                    case "longitude":
                    case "lng":
                    case "lon":
                        longitude = reader.GetDouble();
                        break;
                }
            }
        }

        if (latitude.HasValue && longitude.HasValue)
        {
            return GeographyPoint.Create(latitude.Value, longitude.Value);
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, GeographyPoint value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        writer.WriteNumber("latitude", value.Latitude);
        writer.WriteNumber("longitude", value.Longitude);
        writer.WriteEndObject();
    }
}