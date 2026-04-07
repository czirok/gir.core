using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using GObject;

#nullable enable

namespace NM.Internal;

public abstract partial class KeyfileHandlerDataHandle
{
    public partial KeyfileHandlerDataOwnedHandle OwnedCopy()
    {
        throw new NotImplementedException("OwnedCopy must be implemented in the partial class");
    }
    public partial KeyfileHandlerDataUnownedHandle UnownedCopy()
    {
        throw new NotImplementedException("UnownedCopy must be implemented in the partial class");
    }
}

public partial class KeyfileHandlerDataOwnedHandle : KeyfileHandlerDataHandle
{
    public static partial KeyfileHandlerDataOwnedHandle FromUnowned(IntPtr ptr)
    {
        throw new NotImplementedException("FromUnowned must be implemented in the partial class");
    }

    protected override partial bool ReleaseHandle()
    {
        throw new NotImplementedException("ReleaseHandle must be implemented in the partial class");
    }
}
