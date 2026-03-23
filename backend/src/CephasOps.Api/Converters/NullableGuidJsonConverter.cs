using System.Text.Json;
using System.Text.Json.Serialization;

namespace CephasOps.Api.Converters;

/// <summary>
/// JSON converter for nullable Guid that handles empty strings as null
/// </summary>
public class NullableGuidJsonConverter : JsonConverter<Guid?>
{
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return null;
            }

            if (Guid.TryParse(stringValue, out var guid))
            {
                return guid;
            }
        }

        throw new JsonException($"Unable to convert \"{reader.GetString()}\" to Guid?");
    }

    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.Value.ToString());
        }
    }
}

