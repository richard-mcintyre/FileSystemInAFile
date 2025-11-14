using FileSystemInAFile;

namespace FsDir;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("USAGE: fsdir <fs_filename> <path> <options>");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  /s    Include subdirectories");
            return;
        }

        string fsFileName = args[0];
        string path = args[1];

        if (!Path.Exists(fsFileName))
        {
            Console.WriteLine($"File system file '{fsFileName}' does not exist.");
            return;
        }

        using (FileSystem fs = FileSystem.OpenExisting(fsFileName))
        {
            bool includeSubDirectories = HasOption(args, "/s");

            if (!path.ContainsAny(['^', '*', '?', '$']) && fs.DirectoryExists(path))
                path = FileSystemPath.Combine(path, "*");

            string prevDirectoryName = String.Empty;
            int fileCount = 0;
            int dirCount = 0;
            long totalFileSize = 0;

            foreach (FileSystemEntry entry in GetPathContents(fs, path, includeSubDirectories))
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

    private static bool HasOption(string[] args, string option)
    {
        foreach (string arg in args)
        {
            if (arg.Equals(option, StringComparison.InvariantCultureIgnoreCase))
                return true;
        }

        return false;
    }

}
