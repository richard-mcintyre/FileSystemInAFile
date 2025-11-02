using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemInAFile;
internal class PageFileSystemHeader : Page
{
    #region Header

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Header
    {
        public Header()
        {
        }

        public byte PageKind = (byte)FileSystemInAFile.PageKind.FileSystemHeader;
        public ushort PageSize;
        public uint RootDirectoryPageId;
        public uint FirstAllocationPageId;
    }

    #endregion

    #region Construction

    internal PageFileSystemHeader(Span<byte> contents, PageContentsPool pageContentsPool)
        : base(0, PageKind.FileSystemHeader, pageContentsPool)
    {
        if (contents.Length < PageFileSystemHeader.SizeOfHeader)
            throw new ArgumentOutOfRangeException(nameof(contents));

        _header = MemoryMarshal.Read<Header>(contents);
    }
    
    #endregion

    #region Fields

    private readonly Header _header;

    #endregion

    #region Properties

    internal static int SizeOfHeader => Marshal.SizeOf<Header>();

    internal ushort PageSize => _header.PageSize;

    internal uint RootDirectoryPageId => _header.RootDirectoryPageId;

    internal uint FirstAllocationPageId => _header.FirstAllocationPageId;

    #endregion

    #region Methods

    public static ushort GetPageSize(Span<byte> contents) =>
        MemoryMarshal.Read<Header>(contents).PageSize;

    internal static void InitializeNewPage(Memory<byte> pageContents, 
        ushort pageSize, uint rootDirectoryPageId, uint firstAllocationPageId)
    {
        MemoryMarshal.Write(pageContents.Span, new Header()
        {
            PageSize = pageSize,
            RootDirectoryPageId = rootDirectoryPageId,
            FirstAllocationPageId = firstAllocationPageId
        });
    }

    protected internal override Span<byte> AsSpan() => Array.Empty<byte>();

    #endregion
}
