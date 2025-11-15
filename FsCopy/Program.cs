using System;
using System.Collections.Generic;
using System.Text;
using FileSystemInAFile;
using Fs.Cli.Common;

namespace FsCopy;

internal class Program
{
    static void Main(string[] args)
    {
        OptionDefinitions optionDefs = new OptionDefinitions();
        optionDefs.AddUnnamed("FsPath", "Path to the File System file.");
        optionDefs.AddUnnamed("SourcePath", "Path to the file in the local file system to copy.");
        optionDefs.AddUnnamed("TargetPath", "Path to the file in the File System file to copy the file to .");
        optionDefs.AddNamed("s", "Include subdirectories.");
        optionDefs.AddNamedWithValue("PageSize", "Page size to use when creating a new File System file.  Default is 8192.");

        ProgramArgs? pargs = OptionsParser.TryParse<ProgramArgs>(optionDefs, args);
        if (pargs is null || !pargs.IsValid())
        {
            optionDefs.PrintUsage("FsCopy");
            return;
        }

        Run(pargs);
    }

    private static void Run(ProgramArgs args)
    {
        string sourcePath = args.SourcePath;

        // If the sourcePath is a directory, then we will modifying it to include all files in that directory
        if (Directory.Exists(sourcePath))
            sourcePath = Path.Combine(sourcePath, "*");

        if (!Path.Exists(args.FsPath))
        {
            FileSystem.CreateNew(args.FsPath, args.PageSize).Dispose();
        }

        using (FileSystem fs = FileSystem.OpenExisting(args.FsPath))
        {
            foreach (string sourceFile in GetFileNames(sourcePath, args.IncludeSubdirectories))
            {
                string targetFile = FileSystemPath.Combine(args.TargetPath, 
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
