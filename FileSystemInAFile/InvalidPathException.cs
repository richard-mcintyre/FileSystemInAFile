using System;
using System.Collections.Generic;
using System.Text;

namespace FileSystemInAFile;

public class InvalidPathException : Exception
{
    public InvalidPathException(string path)
        : base($"The path '{path}' is invalid.")
    {
    }
}
