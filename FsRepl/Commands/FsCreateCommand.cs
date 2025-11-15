using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using FileSystemInAFile;
using Fs.Cli.Common.Json;

namespace FsRepl.Commands;

internal class FsCreateCommand : CommandBase<FsCreateCommand.CommandOptions>
{
    internal record CommandOptions(string Path,
        [property: JsonConverter(typeof(UInt16StringJsonConverter))] ushort? PageSize) : ICommandOptions
    {
        public bool IsValid() =>
            !String.IsNullOrWhiteSpace(Path);
    }

    public FsCreateCommand()
        : base("FsCreate", "Creates a new File System file.")
    {
        this.Options.AddUnnamed("Path", "The path to the File System file to create.");
        this.Options.AddNamedWithValue("PageSize", "The page size to use for the File System file (in bytes).");
    }

    protected override CommandResults? Execute(ExecuteSettings settings, CommandOptions options)
    {
        string path = Path.GetFullPath(options.Path);

        settings.StdOut.WriteLine($"Creating {path}...");

        FileSystem? fs = null;
        if (options.PageSize.HasValue)
        {
            fs = FileSystem.CreateNew(path, options.PageSize.Value);
        }
        else
        {
            fs = FileSystem.CreateNew(path);
        }

        return new CommandResults()
        {
            UseFileSystem = fs
        };
    }
}
