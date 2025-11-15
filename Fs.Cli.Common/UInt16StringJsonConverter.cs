using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fs.Cli.Common;

public class UInt16StringJsonConverter : JsonConverter<ushort>
{
    public override ushort Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (ushort.TryParse(reader.GetString(), out ushort value))
                return value;
            throw new JsonException("Invalid ushort value in string.");
        }
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt16(out ushort number))
        {
            return number;
        }
        throw new JsonException("Invalid token for ushort.");
    }

    public override void Write(Utf8JsonWriter writer, ushort value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
