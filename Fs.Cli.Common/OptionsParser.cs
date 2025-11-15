using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fs.Cli.Common;

public class OptionsParser
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    };

    public static bool TryParse(OptionDefinitions defs, string[] args, bool formatted, out string json)
    {
        if (HasHelpOption(args))
        {
            json = String.Empty;
            return false;            
        }

        HashSet<string> unusedArgs = new HashSet<string>(args, StringComparer.InvariantCultureIgnoreCase);

        MemoryStream ms = new MemoryStream();
        using (Utf8JsonWriter writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = formatted }))
        {
            writer.WriteStartObject();

            OptionDefinition[] unnamedDefs = defs.Where(o => o.Kind == OptionKind.Unnamed).ToArray();
            if (args.Length < unnamedDefs.Length)
            {
                json = String.Empty;
                return false;
            }

            string[] unnamedArgValues = args.Take(unnamedDefs.Length).ToArray();

            for (int i = 0; i < unnamedArgValues.Length; i++)
            {
                writer.WritePropertyName(unnamedDefs[i].Name);
                writer.WriteStringValue(unnamedArgValues[i]);

                unusedArgs.Remove(unnamedArgValues[i]);
            }

            foreach (OptionDefinition def in defs.Where(o => o.Kind == OptionKind.Named))
            {
                foreach (string arg in args)
                {
                    if (!String.Equals(arg, $"/{def.Name}", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    writer.WritePropertyName(def.Name);
                    writer.WriteBooleanValue(true);

                    unusedArgs.Remove(arg);
                }
            }

            foreach (OptionDefinition def in defs.Where(o => o.Kind == OptionKind.NamedWithValue))
            {
                foreach (string arg in args)
                {
                    if (!arg.StartsWith($"/{def.Name}=", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    writer.WritePropertyName(def.Name);
                    writer.WriteStringValue(arg.Substring(arg.IndexOf('=') + 1));

                    unusedArgs.Remove(arg);
                }
            }

            writer.WriteEndObject();
        }

        if (unusedArgs.Count > 0)
        {
            json = String.Empty;
            return false;
        }

        json = Encoding.UTF8.GetString(ms.ToArray());
        return true;
    }

    public static T? TryParse<T>(OptionDefinitions defs, string[] args)
    {
        if (TryParse(defs, args, false, out string json))
            return JsonSerializer.Deserialize<T>(json, _jsonOptions)!;

        return default;
    }

    private static bool HasHelpOption(string[] args)
    {
        foreach (string arg in args)
        {
            foreach (string helpArg in new[] { "/help", "-help", "/h", "-h", "/?" })
            {
                if (arg.Equals(helpArg, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}
