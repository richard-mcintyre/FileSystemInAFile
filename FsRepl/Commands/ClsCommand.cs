using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace FsRepl.Commands;

internal class ClsCommand : CommandBaseNoOptions
{
    public ClsCommand()
        : base("Cls", "Clears the console screen.")
    {
    }
    protected override CommandResults? Execute(ExecuteSettings settings)
    {
        Console.Clear();
        return null;
    }
}
