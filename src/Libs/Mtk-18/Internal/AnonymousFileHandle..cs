using System;

namespace Mtk.Internal;

public abstract partial class AnonymousFileHandle
{
    public partial AnonymousFileOwnedHandle OwnedCopy()
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }

    public partial AnonymousFileUnownedHandle UnownedCopy()
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }
}

public partial class AnonymousFileOwnedHandle
{
    public static partial AnonymousFileOwnedHandle FromUnowned(IntPtr ptr)
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }

    protected override partial bool ReleaseHandle()
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }
}
