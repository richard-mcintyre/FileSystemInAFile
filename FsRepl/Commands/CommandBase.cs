using System;
using System.Collections.Generic;
using System.Text;
using Fs.Cli.Common;

namespace FsRepl.Commands;

internal abstract class CommandBase<Toptions> : ICommand
    where Toptions : ICommandOptions
{
    protected CommandBase(string name, string description)
    {
        this.Name = name;
        this.Description = description;
    }

    public string Name { get; }

    public string Description { get; }

    public OptionDefinitions Options { get; } = new OptionDefinitions();

    public CommandResults? Execute(ExecuteSettings settings, string[] args)
    {
        Toptions? options = OptionsParser.TryParse<Toptions>(this.Options, args);

        if (options is null || !options.IsValid())
        {
            this.Options.PrintUsage(this.Name, this.Description);
            return null;
        }

        return Execute(settings, options);
    }

    protected abstract CommandResults? Execute(ExecuteSettings settings, Toptions options);
}
