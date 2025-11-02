using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemInAFile;
public record FileSystemEntry(string Path, string Name, bool IsFile, ulong FileSize);
