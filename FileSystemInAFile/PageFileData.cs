using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemInAFile;
internal class PageFileData : Page
{
    #region Header

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Header
    {
        public Header()
        {
        }

        public byte PageKind = (byte)FileSystemInAFile.PageKind.FileData;
        public uint NextPageId;
    }

    #endregion

    #region Construction

    internal PageFileData(uint pageId, byte[] contents, PageContentsPool contentsPool)
        : base(pageId, PageKind.FileData, contentsPool)
    {
        _contents = contents;       
    }

    #endregion
    
    #region Fields

    private static int _headerSize = Marshal.SizeOf(typeof(Header));

    private readonly byte[] _contents;

    #endregion

    #region Properties

    public static int HeaderSize => _headerSize;

    #endregion

    #region Methods

    public static void InitializeNewPage(Memory<byte> pageContents)
    {
        MemoryMarshal.Write(pageContents.Span, new Header()
        {
            NextPageId = 0,
        });
    }

    public void WriteData(Stream stream)
    {
        Span<byte> data = GetFileDataSpan();

        _ = stream.Read(data);

        this.IsDirty = true;
    }

    private Span<byte> GetFileDataSpan() => AsSpan().Slice(PageFileData.HeaderSize);

    protected internal override Span<byte> AsSpan() => _contents;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _contentsPool.Return(_contents);
        }
    }

    #endregion
}
