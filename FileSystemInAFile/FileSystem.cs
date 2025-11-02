using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FileSystemInAFile;
public class FileSystem : IDisposable
{
    #region Construction

    private FileSystem(Stream stream)
    {
        _pageManager = new PageManager(stream);
    }

    ~FileSystem() =>
        Dispose(false);

    #endregion

    #region Fields

    private const ushort MinPageSize = 512;
    private const ushort MaxPageSize = 65535;

    private readonly PageManager _pageManager;

    #endregion

    #region Methods

    public static FileSystem CreateNew(string path, ushort pageSize = 8192)
    {
        if (pageSize < MinPageSize || pageSize > MaxPageSize)
            throw new ArgumentOutOfRangeException(nameof(pageSize));

        using (Stream stream = File.Open(path, FileMode.CreateNew, FileAccess.ReadWrite))
        {
            Memory<byte> data = new byte[pageSize * 3];

            PageFileSystemHeader.InitializeNewPage(data.Slice(0, pageSize), pageSize, rootDirectoryPageId: 2, firstAllocationPageId: 1);
            PageAllocation.InitializeNewPage(data.Slice(pageSize, pageSize), allocatePageCount: 3);
            PageDirectory.InitializeNewPage(data.Slice(pageSize * 2, pageSize));

            stream.Write(data.Span);
        }

        return FileSystem.Open(path);
    }

    public static FileSystem Open(string path)
    {
        Stream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
        return new FileSystem(stream);
    }

    public void WriteFile(string path, Stream stream)
    {
        string fileName = Path.GetFileName(path);
        path = Path.GetDirectoryName(path)!;

        if (!PageDirectory.IsEntryNameValid(fileName))
            throw new Exception($"Invalid file name: {fileName}");

        IEnumerable<PageDirectory>? parentDirectoryPages = GetPageDirectoryChainForPath(path);
        if (parentDirectoryPages is null)
            throw new Exception($"Invalid path {path}");

        // Check if a directory or file already exists with the same name
        DirectoryEntry? dirEntry = parentDirectoryPages
            .Select(o => o.GetExistingEntries())
            .SelectMany(o => o)
            .FirstOrDefault(o => String.Equals(fileName, o.Name));

        if (dirEntry is not null)
            throw new Exception($"File {fileName} already exists");

        // Find a page with space for an entry
        PageDirectory? pageDirectory = parentDirectoryPages.FirstOrDefault(o => o.HasFreeEntries);
        if (pageDirectory is null)
        {
            // all the pages are full of entries, allocate a new directory page
            pageDirectory = _pageManager.AllocateNewPage<PageDirectory>(
                PageKind.Directory, previousPageId: parentDirectoryPages.Last().Id);
        }

        // Determine how many pages we need to allocate for the file data
        long filePageCount = stream.Length / (_pageManager.PageSize - PageFileData.HeaderSize);
        if (stream.Length % (_pageManager.PageSize - PageFileData.HeaderSize) != 0)
            filePageCount++;

        IEnumerable<PageFileData> filePageChain = _pageManager.AllocateNewPages<PageFileData>(PageKind.FileData, filePageCount);
        foreach (PageFileData pageFileData in filePageChain)
        {
            pageFileData.WriteData(stream);
        }

        // Add the directory entry
        pageDirectory.AddFileEntry(fileName, filePageChain.First().Id, (ulong)stream.Length);

        _pageManager.Flush();
    }

    public IEnumerable<FileSystemEntry> GetDirectoryContents(string path)
    {
        IEnumerable<PageDirectory>? pages = GetPageDirectoryChainForPath(path);
        if (pages is null)
            throw new Exception($"Path does not exist: {path}");

        foreach (DirectoryEntry entry in pages.Select(o => o.GetExistingEntries()).SelectMany(o => o))
        {
            yield return new FileSystemEntry(path, entry.Name, entry.IsFile, entry.FileSize);
        }
    }

    public void CreateDirectory(string path)
    {
        if (path.Length == 0 || path[0] != '\\')
            throw new Exception($"Invalid path: {path}");

        if (path == @"\")
            return;

        string[] nameList = path.Substring(1).Split('\\');
        foreach (string name in nameList)
        {
            if (!PageDirectory.IsEntryNameValid(name))
                throw new Exception($"Invalid directory name: {name}");
        }

        // Get the pages for the root directory
        IEnumerable<PageDirectory> parentDirectoryPages = _pageManager.GetPageChain<PageDirectory>(_pageManager.RootDirectoryPageId);

        foreach (string name in nameList)
        {
            // Check if the directory already exists
            DirectoryEntry? dirEntry = parentDirectoryPages
                .Select(o => o.GetExistingEntries())
                .SelectMany(o => o)
                .FirstOrDefault(o => String.Equals(name, o.Name));

            if (dirEntry is not null)
            {
                parentDirectoryPages = _pageManager.GetPageChain<PageDirectory>(dirEntry.FirstPageId);
                continue;
            }

            // Find a page with space for an entry
            PageDirectory? pageDirectory = parentDirectoryPages.FirstOrDefault(o => o.HasFreeEntries);
            if (pageDirectory is null)
            {
                // all the pages are full of entries, allocate a new directory page
                pageDirectory = _pageManager.AllocateNewPage<PageDirectory>(
                    PageKind.Directory, previousPageId: parentDirectoryPages.Last().Id);
            }
            
            // Allocate a page for the contents of the new directory
            PageDirectory? pageNewDirectory = _pageManager.AllocateNewPage<PageDirectory>(PageKind.Directory);

            // Add the directory entry
            pageDirectory.AddDirectoryEntry(name, pageNewDirectory.Id);

            parentDirectoryPages = [pageNewDirectory];
        }

        _pageManager.Flush();
    }

    private IEnumerable<PageDirectory>? GetPageDirectoryChainForPath(string path)
    {
        // Get the pages for the root directory
        IEnumerable<PageDirectory> parentDirectoryPages = _pageManager.GetPageChain<PageDirectory>(_pageManager.RootDirectoryPageId);

        if (String.Equals(path.Trim(), "\\", StringComparison.Ordinal))
            return parentDirectoryPages;

        string[] nameList = path.Substring(1).Split('\\');

        foreach (string name in nameList)
        {
            // Check if the directory already exists
            DirectoryEntry? dirEntry = parentDirectoryPages
                .Select(o => o.GetExistingEntries())
                .SelectMany(o => o)
                .FirstOrDefault(o => String.Equals(name, o.Name));

            if (dirEntry is not null)
            {
                parentDirectoryPages = _pageManager.GetPageChain<PageDirectory>(dirEntry.FirstPageId);
                continue;
            }

            return null;
        }

        return parentDirectoryPages;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
            _pageManager.Dispose();
    }

    #endregion
}
