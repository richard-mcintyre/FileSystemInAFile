using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemInAFile;
internal enum PageKind
{
    FileSystemHeader = 1,
    Allocation = 2,
    Directory = 3,
    FileData = 4,
}
