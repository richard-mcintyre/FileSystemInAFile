using System;
using System.Collections.Generic;
using System.Text;
using FileSystemInAFile;

namespace FsRepl;

internal class ExecuteSettings
{
    public required FileSystem? FileSystem{ get; init; }
}
