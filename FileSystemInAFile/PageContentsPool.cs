using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemInAFile;
internal class PageContentsPool
{
    #region Construction

    public PageContentsPool(ushort pageSize)
    {
        _pageSize = pageSize;
    }

    #endregion

    #region Fields

    private readonly ushort _pageSize;
    private readonly Queue<byte[]> _pool = new Queue<byte[]>();

    #endregion

    #region Methods

    public byte[] Rent()
    {
        if (_pool.Count == 0)
            return new byte[_pageSize];
        
        byte[] contents = _pool.Dequeue();
        Array.Clear(contents);

        return contents;
    }

    public void Return(byte[] data)
    {
        _pool.Enqueue(data);
    }

    #endregion
}
