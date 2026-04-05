using System;

namespace Mtk.Internal;

public abstract partial class DbusPidfdHandle
{
    public partial DbusPidfdOwnedHandle OwnedCopy()
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }

    public partial DbusPidfdUnownedHandle UnownedCopy()
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }
}

public partial class DbusPidfdOwnedHandle
{
    public static partial DbusPidfdOwnedHandle FromUnowned(IntPtr ptr)
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }

    protected override partial bool ReleaseHandle()
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }
}
