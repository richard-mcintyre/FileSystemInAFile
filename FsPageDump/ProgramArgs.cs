using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Fs.Cli.Common;

namespace FsPageDump;

internal record ProgramArgs(
    string Path, 
    string Pages, 
    [property: JsonPropertyName("hex"),
               JsonConverter(typeof(NullableInt32JsonConverter))] int? BytesPerLine)
{
    public bool IsValid() =>
        !String.IsNullOrWhiteSpace(Path) && 
        !String.IsNullOrWhiteSpace(Pages);
}
