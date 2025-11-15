using System;
using System.Collections.Generic;
using System.Text;
using Fs.Cli.Common;

namespace FsRepl.Commands;

internal class ExitCommand : CommandBaseNoOptions
{
    public ExitCommand()
        : base("Exit", "Exits the REPL.")
    {
    }

    protected override CommandResults? Execute(ExecuteSettings settings)
    {
        return new CommandResults()
        {
            Exit = true
        };
    }
}
