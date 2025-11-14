using System;
using System.Collections.Generic;
using System.Text;

namespace FileSystemInAFile;

[Flags]
public enum FileSystemFileMode
{
    Read = 0b0000_0001,
    ReadWrite = 0b0000_0010,
    CreateNew = 0b0000_0100
}
