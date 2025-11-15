using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Fs.Cli.Common;

namespace FsRepl.Commands;

internal class DebugLaunchCommand : CommandBaseNoOptions
{
    public DebugLaunchCommand()
        : base("DebugLaunch", "Launches the debugger to debug FsRepl.")
    {
    }

    protected override CommandResults? Execute(ExecuteSettings settings)
    {
        return new CommandResults()
        {
            LaunchDebugger = true
        };
    }
}
