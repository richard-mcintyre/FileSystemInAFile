using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemInAFile;
internal record DirectoryEntry(string Name, bool IsFile, ulong FileSize, uint FirstPageId);
