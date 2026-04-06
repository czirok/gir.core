using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using GObject;

#nullable enable

namespace Cogl.Internal;

public abstract partial class DmaBufHandleHandle
{
    public partial DmaBufHandleOwnedHandle OwnedCopy()
    {
        throw new NotSupportedException("Can't create a copy of this handle");
    }

    public partial DmaBufHandleUnownedHandle UnownedCopy()
    {
        throw new NotSupportedException("Can't create a copy of this handle");
    }
}

public partial class DmaBufHandleOwnedHandle : DmaBufHandleHandle
{
    public static partial DmaBufHandleOwnedHandle FromUnowned(IntPtr ptr)
    {
        throw new NotSupportedException("Can't create a copy of this handle");
    }
}
