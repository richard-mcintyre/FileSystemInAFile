using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemInAFile;
internal class FileSystemStream : Stream
{
    #region Construction

    public FileSystemStream(FileSystem fileSystem, FileSystemFileMode fileMode, uint directoryEntryPageId, DirectoryEntry entry)
    {
        _fileSystem = fileSystem;
        _fileMode = fileMode;
        _directoryEntryPageId = directoryEntryPageId;
        _entry = entry;

        _fileDataPerPage = _fileSystem.PageManager.PageSize - PageFileData.HeaderSize;
    }

    #endregion

    #region Fields

    private readonly FileSystem _fileSystem;
    private readonly FileSystemFileMode _fileMode;
    private readonly uint _directoryEntryPageId;
    private DirectoryEntry _entry;
    private readonly int _fileDataPerPage;

    private long _position;

    #endregion

    #region Properties

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => this.IsReadWrite;

    public override long Length => (long)_entry.FileSize;

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    private bool IsReadWrite =>
        (_fileMode & FileSystemFileMode.ReadWrite) != 0;

    #endregion

    #region Methods

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalRead = 0;

        int bytesToRead = count;
        while (bytesToRead > 0)
        {
            int read = InternalRead(buffer, offset, bytesToRead);
            if (read == 0)
                break; // EOF

            offset += read;
            bytesToRead -= read;
            totalRead += read;
        }

        return totalRead;
    }

    private int InternalRead(byte[] buffer, int offset, int count)
    {
        if (_position == this.Length)
            return 0;  // EOF

        // For simplicity we will only read up to the end of the page

        // Determine which page we are going to read from
        int pageIndex = (int)(_position / _fileDataPerPage);
        int pageOffset = (int)(_position % _fileDataPerPage);

        // Determine how much data we can read from the page
        int availableDataOnPage = _fileDataPerPage - pageOffset;

        // We may need to adjust the available data on the page based on the file size
        if ((int)(_entry.FileSize / (ulong)_fileDataPerPage) == pageIndex)
        {
            // We are reading from the last page, so we do need to adjust
            availableDataOnPage = ((int)(_entry.FileSize % (ulong)_fileDataPerPage)) - pageOffset;
        }

        // Get the pages that make up this file
        PageFileData[] pages = _fileSystem.PageManager.GetPageChain<PageFileData>(_entry.FirstPageId).ToArray();

        int toCopy = Math.Min(availableDataOnPage, count);

        pages[pageIndex].InternalGetFileDataSpan()
            .Slice(pageOffset, toCopy)
            .CopyTo(buffer.AsSpan(offset));

        _position += toCopy;

        return toCopy;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!this.CanWrite)
            throw new Exception($"Stream is read-only");

        if (count == 0)
            return;

        // If we need to resize the stream
        if (this.Position + count > this.Length)
            SetLength(this.Position + count);
        
        // Get the pages that make up this file
        PageFileData[] pages = _fileSystem.PageManager.GetPageChain<PageFileData>(_entry.FirstPageId).ToArray();

        while (count > 0)
        {
            // Determine which page we are going to write to
            int pageIndex = (int)(_position / _fileDataPerPage);
            int pageOffset = (int)(_position % _fileDataPerPage);

            Span<byte> pageData = pages[pageIndex].InternalGetFileDataSpan().Slice(pageOffset);

            // If there is enough space in this page to write all the data
            if (pageData.Length > count)
            {
                buffer.AsSpan(offset, count).CopyTo(pageData);
                pages[pageIndex].IsDirty = true;

                _position += count;
                break;
            }

            // Otherwise fill the current page with as much as we can
            int dataToWriteOnPage = pageData.Length;

            buffer.AsSpan(offset, dataToWriteOnPage).CopyTo(pageData);
            pages[pageIndex].IsDirty = true;

            _position += dataToWriteOnPage;

            offset += dataToWriteOnPage;
            count -= dataToWriteOnPage;
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long actualOffset;
        switch (origin)
        {
            case SeekOrigin.Begin:
                actualOffset = offset;
                break;

            case SeekOrigin.Current:
                actualOffset = _position + offset;
                break;

            case SeekOrigin.End:
                actualOffset = this.Length - offset;
                break;

            default:
                throw new ArgumentException(nameof(origin));
        }

        if (actualOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot seek to before the beginning of the stream");

        if (actualOffset > this.Length)
            SetLength(actualOffset);

        _position = actualOffset;
        return _position;
    }

    public override void SetLength(long value)
    {
        if (!this.CanWrite)
            throw new Exception($"Stream is read-only");

        // Determine how many pages are needed for the new size
        uint requiredPageCount = (uint)(value / _fileDataPerPage);
        if(value % _fileDataPerPage != 0)
            requiredPageCount++;

        // If we dont require any pages
        if (requiredPageCount == 0)
        {
            // Free any allocated pages
            if (_entry.FirstPageId != 0)
            {
                _fileSystem.PageManager.FreePageChain(_entry.FirstPageId);
                _entry = _entry with
                {
                    FirstPageId = 0,
                    FileSize = (uint)value
                };
            }
        }
        else if (_entry.FirstPageId == 0)
        {
            // No pages have been allocated, allocate them now
            PageFileData[] pages = _fileSystem.PageManager.AllocateNewPages<PageFileData>(PageKind.FileData, requiredPageCount);
            _entry = _entry with 
            {
                FirstPageId = pages[0].Id ,
                FileSize = (uint)value
            };
        }
        else
        {
            // Get the list of allocated pages
            PageFileData[] pages = _fileSystem.PageManager.GetPageChain<PageFileData>(_entry.FirstPageId).ToArray();

            if (pages.Length == requiredPageCount)
            {
                // Do nothing, we have the right number of pages
            }
            else if (pages.Length < requiredPageCount) // If we need more pages, allocate them
            {
                uint pageCountToAllocate = requiredPageCount - (uint)pages.Length;
                _fileSystem.PageManager.AllocateNewPages<PageFileData>(PageKind.FileData, pageCountToAllocate, pages[^1].Id);
            }
            else // If we have too many pages, free the excess
            {
                // Set the previous page's next page id to 0
                pages[requiredPageCount - 1].SetNextPageId(0);

                // Now free the pages
                _fileSystem.PageManager.FreePageChain(pages[requiredPageCount].Id);
            }

            _entry = _entry with
            {
                FileSize = (uint)value
            };
        }

        // Update directory entry
        PageDirectory pageDirectory = _fileSystem.PageManager.GetPage<PageDirectory>(_directoryEntryPageId)!;
        pageDirectory.UpdateFirstPageIdAndFileSize(_entry.Name, _entry.FirstPageId, _entry.FileSize);
    }

    public override void Flush() =>
        _fileSystem.PageManager.Flush();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Flush();
    }

    #endregion
}
