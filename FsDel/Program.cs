using System;
using System.Collections.Generic;
using System.Text;
using FileSystemInAFile;

namespace FsDel;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("USAGE: fsdel <fs_filename> <path>");
            return;
        }

        string fsFileName = args[0];
        string path = args[1];

        using(FileSystem fs = FileSystem.OpenExisting(fsFileName))
        {
            fs.Delete(path);
        }
    }
}
