using System;
using System.Collections.Generic;
using System.Text;
using Fs.Cli.Common;

namespace FsRepl.Commands;

internal class FsCloseCommand : CommandBaseNoOptions
{
    public FsCloseCommand()
        : base("FsClose", "Closes the current File System.")
    {
    }

    protected override CommandResults? Execute(ExecuteSettings settings)
    {
        return new CommandResults()
        {
            CloseFileSystem = true
        };
    }
}
