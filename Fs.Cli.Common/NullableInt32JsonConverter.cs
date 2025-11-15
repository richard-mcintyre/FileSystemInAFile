using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fs.Cli.Common;

public class NullableInt32JsonConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (Int32.TryParse(reader.GetString(), out int value))
                return value;

            return 0;
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int number))
        {
            return number;
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.True)
        {
            return 0;
        }

        throw new JsonException($"Invalid token for nullable int32.  {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }
        
        writer.WriteNumberValue(value.Value);
    }
}