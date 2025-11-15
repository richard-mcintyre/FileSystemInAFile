using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace FsRepl;

internal static class JsonUtils
{
    public static string FormatJson(string json)
    {
        using (JsonDocument doc = JsonDocument.Parse(json))
        {
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions() 
            { 
                WriteIndented = true 
            });
        }
    }
}
