using System;

namespace Atk.Internal;

public abstract partial class ImplementorHandle
{
    public partial ImplementorOwnedHandle OwnedCopy()
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }

    public partial ImplementorUnownedHandle UnownedCopy()
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }
}

public partial class ImplementorOwnedHandle
{
    public static partial ImplementorOwnedHandle FromUnowned(IntPtr ptr)
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }

    protected override partial bool ReleaseHandle()
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }
}
