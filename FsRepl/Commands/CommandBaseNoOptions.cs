using System;
using System.Collections.Generic;
using System.Text;
using Fs.Cli.Common;

namespace FsRepl.Commands;

internal abstract class CommandBaseNoOptions : CommandBase<CommandBaseNoOptions.NoOptions>
{
    protected CommandBaseNoOptions(string name, string description)
        : base(name, description)
    {
    }

    internal record NoOptions() : ICommandOptions;

    protected sealed override CommandResults? Execute(ExecuteSettings settings, NoOptions args) =>
        Execute(settings);

    protected abstract CommandResults? Execute(ExecuteSettings settings);
}