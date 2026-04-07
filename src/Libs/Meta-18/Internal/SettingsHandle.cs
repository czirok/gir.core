using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using GObject;

#nullable enable

namespace Meta.Internal;


public abstract partial class SettingsHandle
{
    public partial SettingsOwnedHandle OwnedCopy()
    {
        throw new NotImplementedException("OwnedCopy must be implemented in a partial class");
    }
    public partial SettingsUnownedHandle UnownedCopy()
    {
        throw new NotImplementedException("UnownedCopy must be implemented in a partial class");
    }
}


public partial class SettingsOwnedHandle : SettingsHandle
{
    public static partial SettingsOwnedHandle FromUnowned(IntPtr ptr)
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }

    protected override partial bool ReleaseHandle()
    {
        throw new NotSupportedException("Can't create a copy of an implementor handle");
    }
}
