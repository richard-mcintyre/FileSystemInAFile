using System;
using System.Collections.Generic;
using System.Text;
using FileSystemInAFile;

namespace FsRepl;

internal class CommandResults
{
    public bool Exit { get; init; }

    public FileSystem? UseFileSystem { get; init; }

    public bool CloseFileSystem { get; init; }

    public bool LaunchDebugger { get; init; }
}
