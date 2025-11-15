using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Fs.Cli.Common;

namespace FsCopy;

internal record ProgramArgs(
    string FsPath,
    string SourcePath,
    string TargetPath,
    [property: JsonPropertyName("s")] bool IncludeSubdirectories,
    [property: JsonConverter(typeof(UInt16StringJsonConverter))] ushort PageSize = 8192)
{

    public bool IsValid() =>
        !String.IsNullOrWhiteSpace(FsPath) &&
        !String.IsNullOrWhiteSpace(SourcePath) &&
        !String.IsNullOrWhiteSpace(TargetPath);

}
