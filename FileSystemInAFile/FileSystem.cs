using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FileSystemInAFile;
public class FileSystem : IDisposable
{
    #region Construction

    private FileSystem(string path, Stream stream)
    {
        _stream = stream;
        _pageManager = new PageManager(stream);

        this.Path = System.IO.Path.GetFullPath(path);
    }

    ~FileSystem() =>
        Dispose(false);

    #endregion

    #region Fields

    private const ushort MinPageSize = 1024;
    private const ushort MaxPageSize = 65535;

    private readonly Stream _stream;
    private readonly PageManager _pageManager;
    private bool _isDisposed;

    #endregion

    #region Properties

    public string Path { get; }

    internal PageManager PageManager => _pageManager;

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

        return OpenExisting(path);
    }

    public static FileSystem OpenExisting(string path)
    {
        Stream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
        return new FileSystem(path, stream);
    }

    public Stream Open(string path, FileSystemFileMode mode)
    {
        EnsureNotDisposed();

        if (path.StartsWith('$'))
            return OpenSpecial(path);

        EnsurePathIsValid(path);

        string fileName = FileSystemPath.GetFileName(path);
        path = FileSystemPath.GetDirectoryName(path);

        IEnumerable<PageDirectory>? parentDirectoryPages = GetPageDirectoryChainForPath(path);
        if (parentDirectoryPages is null)
            throw new Exception($"Path '{path}' does not exist");

        // Check if a directory or file already exists with the same name
        PageDirectory? pageDirectory = null;
        DirectoryEntry? dirEntry = null;
        foreach (PageDirectory curPageDir in parentDirectoryPages)
        {
            dirEntry = curPageDir
                .GetExistingEntries()
                .FirstOrDefault(o => String.Equals(fileName, o.Name, StringComparison.OrdinalIgnoreCase));

            if (dirEntry is not null)
            {
                pageDirectory = curPageDir;
                break;
            }
        }

        if ((mode & FileSystemFileMode.CreateNew) != 0)
        {
            if (dirEntry is not null)
                throw new Exception($"File {fileName} already exists");

            // Find a page with space for an entry
            pageDirectory = parentDirectoryPages.FirstOrDefault(o => o.HasFreeEntries);
            if (pageDirectory is null)
            {
                // all the pages are full of entries, allocate a new directory page
                pageDirectory = _pageManager.AllocateNewPage<PageDirectory>(
                    PageKind.Directory, previousPageId: parentDirectoryPages.Last().Id);
            }

            // Add the directory entry
            dirEntry = pageDirectory.AddFileEntry(fileName, pageId: 0, fileSize: 0);
        }
        else
        {
            if (dirEntry is null)
                throw new Exception($"File {fileName} does not exist");
        }

        return new FileSystemStream(this, mode, pageDirectory!.Id, dirEntry);
    }

    private Stream OpenSpecial(string path)
    {
        EnsureNotDisposed();

        if (String.Equals(path, SpecialFileNames.FileSystemInfo, StringComparison.Ordinal))
        {
            string data = JsonSerializer.Serialize(new
            {
                Path = this.Path,
                TotalPages = GetPageCount(),
                PageSize = _pageManager.PageSize,
                FileSize = _stream.Length
            });
            
            return new MemoryStream(Encoding.UTF8.GetBytes(data), writable: false);
        }

        if (path.StartsWith(SpecialFileNames.PageInfo, StringComparison.Ordinal))
        {
            uint pageId = UInt32.Parse(path.Substring(SpecialFileNames.PageInfo.Length));

            Page page = _pageManager.GetPage(pageId) 
                ?? throw new Exception($"Page {pageId} does not exist");

            string data = JsonSerializer.Serialize(new
            {
                Id = page.Id,
                Kind = page.Kind.ToString(),
                NextPageId = page.NextPageId,
                IsFree = _pageManager.IsPageFree(page.Id),
            });

            return new MemoryStream(Encoding.UTF8.GetBytes(data), writable: false);
        }

        if (path.StartsWith(SpecialFileNames.PageContents, StringComparison.Ordinal))
        {
            uint pageId = UInt32.Parse(path.Substring(SpecialFileNames.PageContents.Length));

            Page page = _pageManager.GetPage(pageId)
                ?? throw new Exception($"Page {pageId} does not exist");

            return new MemoryStream(page.AsSpan().ToArray(), writable: false);
        }

        throw new Exception($"File {path} does not exist");
    }

    public void Delete(string path)
    {
        EnsureNotDisposed();
        EnsurePathIsValid(path);

        string fileName = FileSystemPath.GetFileName(path);
        path = FileSystemPath.GetDirectoryName(path);

        IEnumerable<PageDirectory>? parentDirectoryPages = GetPageDirectoryChainForPath(path);
        if (parentDirectoryPages is null)
            throw new Exception($"Path '{path}' does not exist");

        // Check if a directory or file already exists with the same name
        PageDirectory? pageDirectory = null;
        DirectoryEntry? dirEntry = null;
        foreach (PageDirectory curPageDir in parentDirectoryPages)
        {
            dirEntry = curPageDir
                .GetExistingEntries()
                .FirstOrDefault(o => String.Equals(fileName, o.Name, StringComparison.OrdinalIgnoreCase));

            if (dirEntry is not null)
            {
                pageDirectory = curPageDir;
                break;
            }
        }

        if (dirEntry is null)
            throw new Exception($"File {fileName} does not exist");

        if (!dirEntry.IsFile)
            throw new Exception($"Path {path} is a directory");

        pageDirectory!.RemoveEntry(dirEntry);
        _pageManager.FreePageChain(dirEntry.FirstPageId);
    }

    public void DeleteDirectory(string path)
    {
        EnsureNotDisposed();
        EnsurePathIsValid(path);

        string fileName = FileSystemPath.GetFileName(path);
        path = FileSystemPath.GetDirectoryName(path);

        IEnumerable<PageDirectory>? parentDirectoryPages = GetPageDirectoryChainForPath(path);
        if (parentDirectoryPages is null)
            throw new Exception($"Path '{path}' does not exist");

        // Check if a directory or file already exists with the same name
        PageDirectory? pageDirectory = null;
        DirectoryEntry? dirEntry = null;
        foreach (PageDirectory curPageDir in parentDirectoryPages)
        {
            dirEntry = curPageDir
                .GetExistingEntries()
                .FirstOrDefault(o => String.Equals(fileName, o.Name, StringComparison.OrdinalIgnoreCase));

            if (dirEntry is not null)
            {
                pageDirectory = curPageDir;
                break;
            }
        }

        if (dirEntry is null)
            throw new Exception($"Directory {fileName} does not exist");

        if (dirEntry.IsFile)
            throw new Exception($"Path {path} is a file");

        pageDirectory!.RemoveEntry(dirEntry);
        _pageManager.FreePageChain(dirEntry.FirstPageId);
    }

    public bool FileExists(string path)
    {
        EnsureNotDisposed();
        EnsurePathIsValid(path);

        string fileName = FileSystemPath.GetFileName(path);
        path = FileSystemPath.GetDirectoryName(path);

        IEnumerable<PageDirectory>? parentDirectoryPages = GetPageDirectoryChainForPath(path);
        if (parentDirectoryPages is null)
            throw new Exception($"Path '{path}' does not exist");

        // Check if a directory or file already exists with the same name
        PageDirectory? pageDirectory = null;
        DirectoryEntry? dirEntry = null;
        foreach (PageDirectory curPageDir in parentDirectoryPages)
        {
            dirEntry = curPageDir
                .GetExistingEntries()
                .FirstOrDefault(o => String.Equals(fileName, o.Name, StringComparison.OrdinalIgnoreCase));

            if (dirEntry is not null)
            {
                pageDirectory = curPageDir;
                break;
            }
        }

        if (dirEntry is null)
            return false;

        return dirEntry.IsFile;
    }

    public bool DirectoryExists(string path)
    {
        EnsureNotDisposed();
        EnsurePathIsValid(path);

        if(path.Length == 1 && path[0] == FileSystemPath.DirectorySeparatorChar)
            return true;

        string fileName = FileSystemPath.GetFileName(path);
        path = FileSystemPath.GetDirectoryName(path);

        IEnumerable<PageDirectory>? parentDirectoryPages = GetPageDirectoryChainForPath(path);
        if (parentDirectoryPages is null)
            throw new Exception($"Path '{path}' does not exist");

        // Check if a directory or file already exists with the same name
        PageDirectory? pageDirectory = null;
        DirectoryEntry? dirEntry = null;
        foreach (PageDirectory curPageDir in parentDirectoryPages)
        {
            dirEntry = curPageDir
                .GetExistingEntries()
                .FirstOrDefault(o => String.Equals(fileName, o.Name, StringComparison.OrdinalIgnoreCase));

            if (dirEntry is not null)
            {
                pageDirectory = curPageDir;
                break;
            }
        }

        if (dirEntry is null)
            return false;

        return !dirEntry.IsFile;
    }

    public IEnumerable<FileSystemEntry> GetDirectoryContents(string path)
    {
        EnsureNotDisposed();
        EnsurePathIsValid(path);

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
        EnsureNotDisposed();
        EnsurePathIsValid(path);

        if (path.Length == 1 && path[0] == FileSystemPath.DirectorySeparatorChar)
            return;

        // Get the pages for the root directory
        IEnumerable<PageDirectory> parentDirectoryPages = _pageManager.GetPageChain<PageDirectory>(_pageManager.RootDirectoryPageId);

        foreach (string name in FileSystemPath.SplitPath(path))
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

        if (path.Length == 1 && path[0] == FileSystemPath.DirectorySeparatorChar)
            return parentDirectoryPages;

        foreach (string name in FileSystemPath.SplitPath(path))
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

    private uint GetPageCount()
    {
        EnsureNotDisposed();
        return (uint)(_stream.Length / _pageManager.PageSize);
    }

    private void EnsurePathIsValid(string path)
    {
        if (!FileSystemPath.IsPathAbsolute(path) || !FileSystemPath.IsPathValid(path))
            throw new InvalidPathException(path);
    }

    private void EnsureNotDisposed()
    {
        if(_isDisposed)
            throw new ObjectDisposedException(nameof(FileSystem));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pageManager?.Flush();
            _pageManager?.Dispose();

            _isDisposed = true;
        }
    }

    #endregion
}
