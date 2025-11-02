using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemInAFile;
[DebuggerDisplay("[{Id}] {Kind}: Next: {NextPageId}")]
internal abstract class Page : IDisposable
{
    #region Construction

    protected Page(uint pageId, PageKind kind, PageContentsPool contentsPool)
    {
        this.Id = pageId;
        this.Kind = kind;
        _contentsPool = contentsPool;
    }

    ~Page() => Dispose(false);

    #endregion

    #region Fields

    protected readonly PageContentsPool _contentsPool;

    #endregion

    #region Properties

    public uint Id { get; }

    public PageKind Kind { get; private set; }

    public uint NextPageId => GetNextPageId();

    public bool IsDirty { get; protected internal set; }

    #endregion

    #region Methods

    public uint GetNextPageId()
    {
        if (!DoesPageKindHaveNextPageId(this.Kind))
            return 0;

        ReadOnlySpan<byte> data = this.AsSpan().Slice(1, 4);
        return BinaryPrimitives.ReadUInt32LittleEndian(data);
    }

    public void SetNextPageId(uint pageId)
    {
        if (!DoesPageKindHaveNextPageId(this.Kind))
            return;

        Span<byte> data = this.AsSpan().Slice(1, 4);
        BinaryPrimitives.WriteUInt32LittleEndian(data, pageId);

        this.IsDirty = true;
    }

    protected internal abstract Span<byte> AsSpan();

    private static bool DoesPageKindHaveNextPageId(PageKind kind) =>
        kind != PageKind.FileSystemHeader;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    #endregion
}
