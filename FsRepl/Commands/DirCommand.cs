using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using FileSystemInAFile;
using Fs.Cli.Common;

namespace FsRepl.Commands;

internal class DirCommand : CommandBase<DirCommand.CommandOptions>
{
    internal record CommandOptions(
        string Path,
        [property: JsonPropertyName("s")] bool IncludeSubdirectories) : ICommandOptions
    {
        public bool IsValid() =>
            !String.IsNullOrWhiteSpace(this.Path);
    }

    public DirCommand()
        : base("Dir", "Lists the contents of a directory.")
    {
        this.Options.AddUnnamed("Path", "Path within the File System to list.");
        this.Options.AddNamed("s", "Include subdirectories.");
    }

    protected override CommandResults? Execute(ExecuteSettings settings, CommandOptions options)
    {
        if (settings.FileSystem is null)
            return null;

        DirectoryListingWriter writer = new DirectoryListingWriter(settings.FileSystem);
        writer.Write(options.Path, options.IncludeSubdirectories, settings.StdOut);

        return null;
    }
}
