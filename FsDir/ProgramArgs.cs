using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FsDir;

internal record ProgramArgs(
    string FsPath,
    string Path,
    [property: JsonPropertyName("s")] bool IncludeSubdirectories)
{

    public bool IsValid() =>
        !String.IsNullOrWhiteSpace(FsPath) &&
        !String.IsNullOrWhiteSpace(Path);

}
