using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using GObject;

#nullable enable

namespace Mtk.Internal;

public abstract partial class DisposeBinHandle
{
    public partial DisposeBinOwnedHandle OwnedCopy()
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }

    public partial DisposeBinUnownedHandle UnownedCopy()
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }
}

public partial class DisposeBinOwnedHandle : DisposeBinHandle
{
    public static partial DisposeBinOwnedHandle FromUnowned(IntPtr ptr)
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }

    protected override partial bool ReleaseHandle()
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }
}
