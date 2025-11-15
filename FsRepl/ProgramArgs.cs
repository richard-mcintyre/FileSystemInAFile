using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Fs.Cli.Common.Json;

namespace FsRepl;

internal record ProgramArgs(
    string Path,
    bool CreateNew,
    [property: JsonPropertyName("PageSize"),
               JsonConverter(typeof(UInt16StringJsonConverter))] ushort CreateNewPageSize = 8192);
