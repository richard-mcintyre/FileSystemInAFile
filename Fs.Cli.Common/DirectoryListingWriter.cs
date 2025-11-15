using System;
using System.Collections.Generic;
using System.Text;
using FileSystemInAFile;

namespace Fs.Cli.Common;

public class DirectoryListingWriter
{
    #region DirectoryContents

    record DirectoryContents(string Path, FileSystemEntry[] Entries)
    {
        public int FileCount => this.Entries.Count(e => e.IsFile);

        public int DirectoryCount => this.Entries.Count(e => !e.IsFile);

        public long TotalFileSize => this.Entries.Where(e => e.IsFile).Sum(e => (long)e.FileSize);
    }

    #endregion

    #region Construction

    public DirectoryListingWriter(FileSystem fs)
    {
        _fs = fs ?? throw new ArgumentNullException(nameof(fs));
    }

    #endregion

    #region Fields

    private readonly FileSystem _fs;

    #endregion

    #region Properties

    public string FileNameColor { get; set; } = AnsiHelper.FgColor(AnsiFgColors.BrightGreen);

    public string DirectoryColor { get; set; } = AnsiHelper.FgColor(AnsiFgColors.BrightBlue);

    #endregion

    #region Methods

    public void Write(string path, bool includeSubDirectories, TextWriter writer)
    {
        if (!path.ContainsAny(['^', '*', '?', '$']) && _fs.DirectoryExists(path))
        {
            path = FileSystemPath.Combine(path, "*");
        }

        foreach (DirectoryContents dirContents in GetPathContents(path, includeSubDirectories))
        {
            writer.WriteLine();
            writer.WriteLine($"Directory of {dirContents.Path}");
            writer.WriteLine();

            foreach (FileSystemEntry entry in dirContents.Entries)
            {
                if (entry.IsFile)
                {
                    writer.WriteLine($"      {entry.FileSize,10:N0} {this.FileNameColor}{entry.Name}{AnsiHelper.Reset}");
                }
                else
                {
                    writer.WriteLine($"<DIR>            {this.DirectoryColor}{entry.Name}{AnsiHelper.Reset}");
                }
            }

            writer.WriteLine();
            writer.WriteLine($"{dirContents.FileCount} File(s) {dirContents.DirectoryCount} Dir(s) {dirContents.TotalFileSize:N0} bytes");
        }
    }

    private IEnumerable<DirectoryContents> GetPathContents(string path, bool includeSubDirectories)
    {
        string fileName = FileSystemPath.GetFileName(path);
        path = FileSystemPath.GetDirectoryName(path);

        Stack<string> stack = new Stack<string>();
        stack.Push(path);

        while (stack.Count > 0)
        {
            string curPath = stack.Pop();

            List<FileSystemEntry> matchedEntries = new List<FileSystemEntry>();
            foreach (FileSystemEntry entry in _fs.GetDirectoryContents(curPath).OrderBy(o => o.Name))
            {
                if (includeSubDirectories && !entry.IsFile)
                {
                    stack.Push(FileSystemPath.Combine(entry.Path, entry.Name));
                }

                if (FileSystemPath.DoesFileNameMatchWildcard(entry.Name, fileName))
                {
                    matchedEntries.Add(entry);
                }
            }

            yield return new DirectoryContents(curPath, matchedEntries.ToArray());
        }
    }

    #endregion
}
