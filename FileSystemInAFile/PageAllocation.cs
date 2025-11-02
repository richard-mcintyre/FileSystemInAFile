using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace FileSystemInAFile;
internal class PageAllocation : Page
{
    #region Header

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Header
    {
        public Header()
        {
        }

        public byte PageKind = (byte)FileSystemInAFile.PageKind.Allocation;
        public uint NextPageId;
    }

    #endregion

    #region Construction

    internal PageAllocation(uint pageId, byte[] contents, PageContentsPool contentsPool, bool isDirty = false)
        : base(pageId, PageKind.Allocation, contentsPool)
    {
        _contents = contents;
        
        this.IsDirty = isDirty;
    }

    #endregion

    #region Fields

    private static int HeaderSize = Marshal.SizeOf(typeof(Header));

    private readonly byte[] _contents;
    private bool _hasFreePages = true;

    #endregion

    #region Properties

    public uint MaxNumberOfPages => (uint)((_contents.Length - HeaderSize) * 8);

    #endregion

    #region Methods

    internal static void InitializeNewPage(Memory<byte> pageContents, int allocatePageCount)
    {
        MemoryMarshal.Write(pageContents.Span, new Header());

        for (int i = 0; i < allocatePageCount; i++)
        {
            (int offset, int bit) = CalcOffsetAndBit(i);
            pageContents.Span[offset] |= (byte)(1 << bit);
        }
    }

    public bool IsPageFree(uint offsetPageId)
    {
        (int offset, int bit) = CalcOffsetAndBit((int)offsetPageId);
        return (_contents[offset] & (byte)(1 << bit)) == 0;
    }

    public bool IsPageUsed(uint offsetPageId) =>
        !IsPageFree(offsetPageId);

    public void MarkPageAsFree(uint offsetPageId) =>
        MarkPageAs(offsetPageId, asFree: true);

    public void MarkPageAsUsed(uint offsetPageId) =>
        MarkPageAs(offsetPageId, asFree: false);

    public uint? GetFreePageId()
    {
        if (!_hasFreePages)
            return null;

        for (int i = HeaderSize; i < _contents.Length; i++)
        {
            if (_contents[i] == 255)
                continue;

            return (uint)(((i - HeaderSize) * 8) + GetFirstFreeBit(_contents[i]));
        }

        _hasFreePages = false;
        return null;
    }

    private static int GetFirstFreeBit(byte data)
    {
        for (int i = 0; i < 8; i++)
        {
            if ((data & (byte)(1 << i)) == 0)
                return i;
        }

        throw new Exception("No free bit");
    }

    private void MarkPageAs(uint offsetPageId, bool asFree)
    {
        (int offset, int bit) = CalcOffsetAndBit((int)offsetPageId);

        if (asFree)
        {
            _contents[offset] &= (byte)~(1 << bit);
            _hasFreePages = true;
        }
        else
        {
            _contents[offset] |= (byte)(1 << bit);
        }
        this.IsDirty = true;
    }

    private static (int offset, int bit) CalcOffsetAndBit(int offsetPageId) =>
        (HeaderSize + (offsetPageId / 8), offsetPageId % 8);

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
