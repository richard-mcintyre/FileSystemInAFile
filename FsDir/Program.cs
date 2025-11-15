using System.Text.Json.Serialization;
using FileSystemInAFile;
using Fs.Cli.Common;

namespace FsDir;

internal class Program
{
    static void Main(string[] args)
    {
        OptionDefinitions optionDefs = new OptionDefinitions();
        optionDefs.AddUnnamed("FsPath", "Path to the File System file.");
        optionDefs.AddUnnamed("Path", "Path within the File System to list.");
        optionDefs.AddNamed("s", "Include subdirectories.");

        ProgramArgs? pargs = OptionsParser.TryParse<ProgramArgs>(optionDefs, args);
        if (pargs is null || !pargs.IsValid())
        {
            optionDefs.PrintUsage("FsDir", "Lists the contents of a directory in a File System file.");
            return;
        }

        Run(pargs);
    }

    private static void Run(ProgramArgs args)
    {        
        if (!Path.Exists(args.FsPath))
        {
            Console.WriteLine($"File system file '{args.FsPath}' does not exist.");
            return;
        }

        using (FileSystem fs = FileSystem.OpenExisting(args.FsPath))
        {
            string path = args.Path;
            if (!path.ContainsAny(['^', '*', '?', '$']) && fs.DirectoryExists(path))
            {
                path = FileSystemPath.Combine(path, "*");
            }

            string prevDirectoryName = String.Empty;
            int fileCount = 0;
            int dirCount = 0;
            long totalFileSize = 0;

            foreach (FileSystemEntry entry in GetPathContents(fs, path, args.IncludeSubdirectories))
            {
                if(!String.Equals(prevDirectoryName, entry.Path, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!String.IsNullOrEmpty(prevDirectoryName))
                    {
                        Console.WriteLine($"     {fileCount} File(s) {totalFileSize:N0} bytes");
                        fileCount = 0;
                        dirCount = 0;
                        totalFileSize = 0;
                    }

                    prevDirectoryName = entry.Path;
                    Console.WriteLine();
                    Console.WriteLine($"Directory of {prevDirectoryName}");
                    Console.WriteLine();
                }

                if (entry.IsFile)
                {
                    Console.WriteLine($"      {entry.FileSize,10:N0} {entry.Name}");
                    fileCount++;
                    totalFileSize += (long)entry.FileSize;
                }
                else
                {
                    Console.WriteLine($"<DIR>            {entry.Name}");
                    dirCount++;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"{fileCount} File(s) {dirCount} Dir(s) {totalFileSize:N0} bytes");
        }
    }

    private static IEnumerable<FileSystemEntry> GetPathContents(FileSystem fs, string path, bool includeSubDirectories)
    {
        string fileName = FileSystemPath.GetFileName(path);
        path = FileSystemPath.GetDirectoryName(path);

        Stack<string> stack = new Stack<string>();
        stack.Push(path);

        while (stack.Count > 0)
        {
            string curPath = stack.Pop();

            foreach (FileSystemEntry entry in fs.GetDirectoryContents(curPath).OrderBy(o => o.Name))
            {
                if (includeSubDirectories && !entry.IsFile)
                {
                    stack.Push(FileSystemPath.Combine(entry.Path, entry.Name));
                }

                if (FileSystemPath.DoesFileNameMatchWildcard(entry.Name, fileName))
                {
                    yield return entry;
                }
            }
        }
    }
}
