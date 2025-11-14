using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemInAFile;
internal class PageManager : IDisposable
{
    #region Construction

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    internal PageManager(Stream stream)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        _stream = stream;

        _fileSystemHeaderPage = ReadFileSystemHeaderPageAndInitializeContentsPool();
        _pageCache.Add(0, _fileSystemHeaderPage);
    }

    ~PageManager() =>
        Dispose(false);

    #endregion

    #region Fields

    private readonly Stream _stream;
    private readonly PageFileSystemHeader _fileSystemHeaderPage;
    private PageContentsPool _pageContentsPool;
    private Dictionary<uint, Page> _pageCache = new Dictionary<uint, Page>();

    #endregion

    #region Properties

    public ushort PageSize => _fileSystemHeaderPage.PageSize;

    public uint RootDirectoryPageId => _fileSystemHeaderPage.RootDirectoryPageId;

    public uint FirstAllocationPageId => _fileSystemHeaderPage.FirstAllocationPageId;

    #endregion

    #region Methods

    public Page? GetPage(uint pageId)
    {
        if (_pageCache.TryGetValue(pageId, out Page? page))
            return page;

        if (_stream.Length >= (pageId + 1) * _fileSystemHeaderPage.PageSize)
            page = ReadPage(pageId);

        return page;
    }

    public T? GetPage<T>(uint pageId)
        where T : Page
    {
        Page? page = GetPage(pageId);
        if (page is null)
            return null;

        T? result = page as T;
        if (result is null)
            throw new Exception($"Attempt to retrieve a pageId {pageId} as a {typeof(T).Name} but its a {page.GetType().Name}");

        return result;
    }

    public IEnumerable<T> GetPageChain<T>(uint firstPageId)
        where T : Page
    {
        T? page = GetPage<T>(firstPageId);
        if (page is null)
            throw new ArgumentOutOfRangeException(nameof(page));

        yield return page;

        uint nextPageId = page.GetNextPageId();
        while (nextPageId != 0)
        {
            page = GetPage<T>(nextPageId);
            if (page is null)
                throw new Exception("Page chain is corrupt");

            yield return page;

            nextPageId = page.GetNextPageId();
        }
    }

    public T[] AllocateNewPages<T>(PageKind kind, long pageCount, uint previousPageId = 0)
        where T : Page
    {
        if (pageCount == 0)
            return Array.Empty<T>();

        List<T> pages = new List<T>();

        Page prevPage = AllocateNewPage<T>(kind, previousPageId);
        pages.Add((T)prevPage);

        for (long i = 1; i < pageCount; i++)
        {
            Page page = AllocateNewPage<T>(kind, prevPage.Id);
            prevPage = page;

            pages.Add((T)page);
        }

        return pages.ToArray();
    }

    public T AllocateNewPage<T>(PageKind kind, uint previousPageId = 0)
        where T : Page
    {
        IEnumerable<PageAllocation> allocPageChain = GetPageChain<PageAllocation>(this.FirstAllocationPageId).ToArray();

        uint basePageId = 0;
        uint? freePageId = null;
        foreach (PageAllocation pageAlloc in allocPageChain)
        {
            freePageId = pageAlloc.GetFreePageId();
            if (freePageId is not null)
            {
                pageAlloc.MarkPageAsUsed(freePageId.Value);
                freePageId += basePageId;
                break;
            }

            basePageId += pageAlloc.MaxNumberOfPages;
        }

        // If all the pages have been allocated
        if (freePageId is null)
        {
            // Determine the next page id
            uint maxAllocsPerPage = allocPageChain.First().MaxNumberOfPages;

            uint pageId = (uint)(maxAllocsPerPage * allocPageChain.Count());

            byte[] bytes = new byte[_fileSystemHeaderPage.PageSize];
            PageAllocation.InitializeNewPage(bytes, 1);
            _pageCache.Add(pageId, new PageAllocation(pageId, bytes, _pageContentsPool, isDirty: true));

            allocPageChain.Last().SetNextPageId(pageId);

            return AllocateNewPage<T>(kind, previousPageId);
        }

        byte[] pageContents = _pageContentsPool.Rent();
        Page? allocatedPage = null;
        switch (kind)
        {
            case PageKind.Directory:
                PageDirectory.InitializeNewPage(pageContents);
                allocatedPage = new PageDirectory(freePageId.Value, pageContents, _pageContentsPool);
                break;

            case PageKind.FileData:
                PageFileData.InitializeNewPage(pageContents);
                allocatedPage = new PageFileData(freePageId.Value, pageContents, _pageContentsPool);
                break;

            default:
                throw new Exception($"Page kind {kind} cannot be allocated");
        }

        // Make the previous page point to the new allocated page
        if (previousPageId != 0)
        {
            Page? prevPage = GetPage(previousPageId);
            if (prevPage is null)
                throw new Exception($"Invalid previous page");

            prevPage.SetNextPageId(freePageId.Value);
        }

        allocatedPage.IsDirty = true;
        _pageCache[allocatedPage.Id] = allocatedPage;

        return (allocatedPage as T)!;
    }

    public void FreePageChain(uint fromPageId)
    {
        IEnumerable<PageAllocation> allocPageChain = GetPageChain<PageAllocation>(this.FirstAllocationPageId).ToArray();

        while (fromPageId != 0)
        {
            uint nextPageId = GetPage<Page>(fromPageId)!.GetNextPageId();

            // Determine which allocation page we should be updating
            uint basePageId = 0;
            foreach (PageAllocation pageAlloc in allocPageChain)
            {
                if (fromPageId >= basePageId && fromPageId < basePageId + pageAlloc.MaxNumberOfPages)
                {
                    uint offsetPageId = fromPageId - basePageId;
                    pageAlloc.MarkPageAsFree(offsetPageId);
                    break;
                }

                basePageId += pageAlloc.MaxNumberOfPages;
            }

            fromPageId = nextPageId;
        }
    }

    public bool IsPageFree(uint pageId)
    {
        IEnumerable<PageAllocation> allocPageChain = GetPageChain<PageAllocation>(this.FirstAllocationPageId).ToArray();
        uint basePageId = 0;
        foreach (PageAllocation pageAlloc in allocPageChain)
        {
            if (pageId >= basePageId && pageId < basePageId + pageAlloc.MaxNumberOfPages)
            {
                uint offsetPageId = pageId - basePageId;
                return pageAlloc.IsPageFree(offsetPageId);
            }

            basePageId += pageAlloc.MaxNumberOfPages;
        }
        return false;
    }


    private PageFileSystemHeader ReadFileSystemHeaderPageAndInitializeContentsPool()
    {
        _stream.Seek(0, SeekOrigin.Begin);
        
        byte[] data = new byte[PageFileSystemHeader.SizeOfHeader];
        _ = _stream.Read(data, 0, data.Length);

        ushort pageSize = PageFileSystemHeader.GetPageSize(data);
        _pageContentsPool = new PageContentsPool(pageSize);

        return new PageFileSystemHeader(data, _pageContentsPool);
    }

    private Page ReadPage(uint pageId)
    {
        if (pageId == 0)
            return _fileSystemHeaderPage;

        byte[] contents;

        checked
        {
            _stream.Seek((long)pageId * _fileSystemHeaderPage.PageSize, SeekOrigin.Begin);
            contents = _pageContentsPool.Rent();
            _ = _stream.Read(contents);
        }

        Page page;
        switch ((PageKind)contents[0])
        {
            case PageKind.Allocation:
                page = new PageAllocation(pageId, contents, _pageContentsPool);
                break;

            case PageKind.Directory:
                page = new PageDirectory(pageId, contents, _pageContentsPool);
                break;

            case PageKind.FileData:
                page = new PageFileData(pageId, contents, _pageContentsPool);
                break;

            default:
                throw new Exception($"Unknown page kind: {contents[0]}");
        }

        _pageCache.Add(pageId, page);

        return page;
    }

    public void Flush()
    {
        Page[] dirtyPages = _pageCache.Values.Where(o => o.IsDirty).ToArray();
        if (dirtyPages.Length == 0)
            return;
        
        uint maxDirtyPageId = dirtyPages.Max(o => o.Id);

        checked
        {
            long requiredFileSize = (long)maxDirtyPageId * _fileSystemHeaderPage.PageSize;
            if (_stream.Length < requiredFileSize)
                _stream.SetLength(requiredFileSize);

            foreach (Page page in dirtyPages)
            {
                _stream.Seek((long)page.Id * _fileSystemHeaderPage.PageSize, SeekOrigin.Begin);
                _stream.Write(page.AsSpan());

                page.IsDirty = false;
            }
        }

        if (_pageCache.Count > 10000)
        {
            // Drop all file data pages
            foreach (Page page in _pageCache.Values)
            {
                if (page.Kind == PageKind.FileSystemHeader || page.Kind == PageKind.Allocation)
                    continue;

                page.Dispose();
            }

            _pageCache = new Dictionary<uint, Page>(
                _pageCache.Values
                    .Where(o => o.Kind == PageKind.FileSystemHeader || o.Kind == PageKind.Allocation)
                    .Select(o => new KeyValuePair<uint, Page>(o.Id, o)));
        }

        _stream.Flush();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
            _stream?.Dispose();
    }

    #endregion
}
