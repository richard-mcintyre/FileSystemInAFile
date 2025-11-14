using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using FileSystemInAFile;

namespace FsCopy;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("USAGE: fscopy <fs_filename> <source_path> <target_path> <options>");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  /s                  Include subdirectories");
            Console.WriteLine("  /pagesize=<size>    Specify page size");
            return;
        }

        string fsFileName = args[0];
        string sourcePath = Path.GetFullPath(args[1]);
        string targetPath = args[2];
        
        // If the sourcePath is a directory, then we will modifying it to include all files in that directory
        if (Directory.Exists(sourcePath))
            sourcePath = Path.Combine(sourcePath, "*");

        if (!Path.Exists(fsFileName))
        {
            ushort pageSize = TryGetOptionValue(args, "/pagesize", 8192);
            FileSystem.CreateNew(fsFileName, pageSize).Dispose();
        }

        using (FileSystem fs = FileSystem.OpenExisting(fsFileName))
        {
            bool includeSubDirectories = HasOption(args, "/s");

            foreach (string sourceFile in GetFileNames(sourcePath, includeSubDirectories))
            {
                string targetFile = FileSystemPath.Combine(targetPath, 
                    Path.GetRelativePath(Path.GetDirectoryName(sourcePath)!, sourceFile)
                        .Replace(Path.DirectorySeparatorChar, FileSystemPath.DirectorySeparatorChar));

                Console.WriteLine($"{sourceFile} => {targetFile}");

                using (Stream sourceStream = File.OpenRead(sourceFile))
                {
                    fs.CreateDirectory(FileSystemPath.GetDirectoryName(targetFile));
                    using (Stream targetStream = fs.Open(targetFile, FileSystemFileMode.CreateNew | FileSystemFileMode.ReadWrite))
                    {
                        sourceStream.CopyTo(targetStream);
                    }
                }
            }
        }
    }

    private static bool HasOption(string[] args, string option)
    {
        foreach (string arg in args)
        {
            if (arg.Equals(option, StringComparison.InvariantCultureIgnoreCase))
                return true;
        }

        return false;
    }

    private static ushort TryGetOptionValue(string[] args, string option, ushort defaultValue)
    {
        foreach (string arg in args)
        {
            if (arg.StartsWith($"{option}=", StringComparison.InvariantCultureIgnoreCase))
            {
                string valueStr = arg.Substring(option.Length + 1);
                if (UInt16.TryParse(valueStr, out ushort value))
                    return value;
            }
        }
        return defaultValue;
    }

    private static IEnumerable<string> GetFileNames(string sourcePath, bool includeSubDirectories)
    {
        string fileNamePattern = Path.GetFileName(sourcePath);

        Stack<string> stack = new Stack<string>();
        stack.Push(Path.GetDirectoryName(sourcePath)!);

        while (stack.Count > 0)
        {
            string path = stack.Pop();

            foreach (string file in Directory.GetFiles(path))
            {
                if (FileSystemPath.DoesFileNameMatchWildcard(Path.GetFileName(file), fileNamePattern))
                    yield return file;
            }

            if (includeSubDirectories)
            {
                foreach (string dir in Directory.GetDirectories(path))
                {
                    stack.Push(Path.Combine(path, dir));
                }
            }
        }
    }
}
