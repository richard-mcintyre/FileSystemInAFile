using System;
using System.Collections.Generic;
using System.Text;

namespace FsRepl.Commands;

internal class HelpCommand : CommandBaseNoOptions
{
    public HelpCommand()
        : base("Help", "Displays available commands.")
    {
    }

    protected override CommandResults? Execute(ExecuteSettings settings)
    {
        int maxCommandNameLength = Program.Commands.Max(o => o.Name.Length);

        Console.WriteLine();
        Console.WriteLine("Available commands:");
        foreach (ICommand command in Program.Commands.OrderBy(o => o.Name))
        {
            Console.WriteLine($"   {command.Name.PadRight(maxCommandNameLength)} - {command.Description}");
        }

        return default;
    }
}
