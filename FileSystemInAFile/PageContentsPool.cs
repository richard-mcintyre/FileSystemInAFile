using System;
using System.Buffers;
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
    private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

    #endregion

    #region Methods

    public byte[] Rent() => _pool.Rent(_pageSize);

    public void Return(byte[] data) => _pool.Return(data);

    #endregion
}
