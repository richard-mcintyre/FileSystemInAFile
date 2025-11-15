using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using FileSystemInAFile;

namespace FsRepl.Commands;

internal class FsOpenCommand : CommandBase<FsOpenCommand.CommandOptions>
{
    internal record CommandOptions(string Path) : ICommandOptions
    {
        public bool IsValid() =>
            !String.IsNullOrWhiteSpace(Path);
    }

    public FsOpenCommand()
        : base("FsOpen", "Opens an existing File System file.")
    {
        this.Options.AddUnnamed("Path", "The path to the existing File System file to open.");
    }

    protected override CommandResults? Execute(ExecuteSettings settings, CommandOptions options)
    {
        string path = Path.GetFullPath(options.Path);
        FileSystem fs = FileSystem.OpenExisting(path);

        return new CommandResults()
        {
            UseFileSystem = fs
        };
    }
}
