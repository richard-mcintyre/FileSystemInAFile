using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace Fs.Cli.Common.Options;

public class OptionDefinitions : List<OptionDefinition>
{
    /// <summary>
    /// Adds an option that must appear after the program/command name
    /// </summary>
    public OptionDefinitions AddUnnamed(string name, string description)
    {
        Add(new OptionDefinition(OptionKind.Unnamed, name, description));
        return this;
    }

    /// <summary>
    /// Adds an option that starts with a dash or slash.
    /// </summary>
    public OptionDefinitions AddNamed(string name, string description)
    {
        Add(new OptionDefinition(OptionKind.Named, name, description));
        return this;
    }

    /// <summary>
    /// Adds an option that starts with a dash or slash and takes a value. (e.g. /testopt=1234)
    /// </summary>
    public OptionDefinitions AddNamedWithValue(string name, string description)
    {
        Add(new OptionDefinition(OptionKind.NamedWithValue, name, description));
        return this;
    }

    public void PrintUsage(string name) =>
        PrintUsage(name, String.Empty);

    public void PrintUsage(string name, string description)
    {
        StringBuilder sb = new StringBuilder();

        if (!String.IsNullOrWhiteSpace(description))
        {
            sb.AppendLine(description);
            sb.AppendLine();
        }

        sb.Append($"Usage: {name}");

        foreach (OptionDefinition option in this.Where(o => o.Kind == OptionKind.Unnamed))
        {
            sb.Append($" <{option.Name}>");
        }

        if (this.Any(o => o.Kind != OptionKind.Unnamed))
        {
            sb.Append(" [options]");
        }

        sb.AppendLine();

        Console.WriteLine(sb);

        sb.Clear();
        foreach (OptionDefinition option in this.Where(o => o.Kind == OptionKind.Unnamed))
        {
            sb.AppendLine($"{option.Name}:");
            sb.AppendLine($"  {option.Description}");
            sb.AppendLine();
        }
        Console.Write(sb);

        sb.Clear();

        OptionDefinition[] namedOptions = this.Where(o => o.Kind != OptionKind.Unnamed).ToArray();
        if (namedOptions.Length > 0)
        {
            int maxNamedOptionLength = namedOptions.Max(o => o.Kind == OptionKind.NamedWithValue ? o.Name.Length + "=<value>".Length : o.Name.Length);

            sb.AppendLine("Options:");
            foreach (OptionDefinition option in namedOptions)
            {
                string? tmp = null;
                switch (option.Kind)
                {
                    case OptionKind.Named:
                        tmp = $"{option.Name}";
                        break;

                    case OptionKind.NamedWithValue:
                        tmp = $"{option.Name}=<value>";
                        break;
                }

                if (tmp is null)
                    continue;

                sb.AppendLine($"  /{tmp.PadRight(maxNamedOptionLength)}  {option.Description}");
            }
            Console.Write(sb);
        }
    }
}


