using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FsDel;

internal record ProgramArgs(
    string FsPath,
    string Path)
{

    public bool IsValid() =>
        !String.IsNullOrWhiteSpace(FsPath) &&
        !String.IsNullOrWhiteSpace(Path);

}

