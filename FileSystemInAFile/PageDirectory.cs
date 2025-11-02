using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemInAFile;
internal class PageDirectory : Page
{
    #region Header
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Header
    {
        public Header()
        {
        }

        public byte PageKind = (byte)FileSystemInAFile.PageKind.Directory;
        public uint NextPageId;
        public ushort NumberOfEntries;
    }

    #endregion

    #region Entry

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct Entry
    {
        public byte NameByteCount;
        public fixed byte Name[MaxEntryNameByteSize];
        public byte Flags;
        public ulong FileSize;
        public uint FirstPageId;
    }

    #endregion

    #region EntryFlags

    [Flags]
    enum EntryFlags
    {
        None = 0,
        File = 0b0000_0001,
    }

    #endregion

    #region Construction

    internal PageDirectory(uint pageId, byte[] contents, PageContentsPool contentsPool)
        : base(pageId, PageKind.Directory, contentsPool)
    {
        _contents = contents;
    }

    #endregion

    #region Fields

    public const int MaxEntryNameByteSize = 128;     // Since we are using UTF16 it should be a multiple of two

    private static int HeaderSize = Marshal.SizeOf<Header>();
    private static int EntrySize = Marshal.SizeOf<Entry>();

    private readonly byte[] _contents;

    #endregion

    #region Properties

    public ushort MaxEntries => (ushort)((_contents.Length - HeaderSize) / EntrySize);

    public ushort UsedEntries
    {
        get => GetUsedEntries();
        private set
        {
            Span<byte> span = this.AsSpan().Slice(5, 2);
            BinaryPrimitives.WriteUInt16LittleEndian(span, value);            
        }
    }

    public bool HasFreeEntries => (MaxEntries - UsedEntries) > 0;

    #endregion

    #region Methods

    public DirectoryEntry AddDirectoryEntry(string name, uint pageId)
    {
        if (this.UsedEntries >= this.MaxEntries)
            throw new Exception("Directory page is full");

        DirectoryEntry entry = new DirectoryEntry(name, IsFile: false, FileSize: 0, pageId);

        DirectoryEntry[] existingEntries = GetExistingEntries();
        UpdateContentsWithEntries(existingEntries.Append(entry));

        return entry;
    }

    public DirectoryEntry AddFileEntry(string name, uint pageId, ulong fileSize)
    {
        if (this.UsedEntries >= this.MaxEntries)
            throw new Exception("Directory page is full");

        DirectoryEntry entry = new DirectoryEntry(name, IsFile: true, FileSize: fileSize, pageId);

        DirectoryEntry[] existingEntries = GetExistingEntries();
        UpdateContentsWithEntries(existingEntries.Append(entry));

        return entry;
    }

    unsafe public DirectoryEntry[] GetExistingEntries()
    {
        byte[] buf = new byte[EntrySize];

        DirectoryEntry[] resultList = new DirectoryEntry[this.UsedEntries];
        for (int i = 0; i < this.UsedEntries; i++)
        {
            Entry entry = MemoryMarshal.Read<Entry>(AsSpan().Slice(HeaderSize + (i * EntrySize), EntrySize));
            
            resultList[i] = new DirectoryEntry(
                Encoding.Unicode.GetString(entry.Name, entry.NameByteCount), 
                IsFile: (entry.Flags & (byte)EntryFlags.File) != 0,
                FileSize: entry.FileSize,
                entry.FirstPageId);
        }

        return resultList;
    }

    private unsafe void UpdateContentsWithEntries(IEnumerable<DirectoryEntry> entries)
    {
        for (int i = 0; i < entries.Count(); i++)
        {
            DirectoryEntry cur = entries.ElementAt(i);

            Entry entry = new Entry();
            entry.Flags = (byte)(cur.IsFile ? EntryFlags.File : EntryFlags.None);
            entry.FirstPageId = cur.FirstPageId;
            entry.FileSize = cur.FileSize;

            byte[] buf = Encoding.Unicode.GetBytes(entries.ElementAt(i).Name);
            entry.NameByteCount = (byte)buf.Length;
            for(int x=0; x<buf.Length; x++)
                entry.Name[x] = buf[x];

            Span<byte> span = AsSpan().Slice(HeaderSize + (i * EntrySize), EntrySize);
            MemoryMarshal.Write(span, entry);
        }

        this.UsedEntries = (ushort)entries.Count();
        this.IsDirty = true;
    }

    public static bool IsEntryNameValid(string name) =>
        !String.IsNullOrWhiteSpace(name) &&
        Encoding.Unicode.GetByteCount(name.Trim()) < MaxEntryNameByteSize;

    private ushort GetUsedEntries()
    {
        ReadOnlySpan<byte> bytes = AsSpan().Slice(5, 2);
        return BinaryPrimitives.ReadUInt16LittleEndian(bytes);
    }

    public static void InitializeNewPage(Memory<byte> pageContents)
    {
        MemoryMarshal.Write(pageContents.Span, new Header()
        {
            NextPageId = 0,
            NumberOfEntries = 0
        });
    }

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
