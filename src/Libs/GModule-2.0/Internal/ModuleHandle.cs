using System;
using System.Runtime.InteropServices;

namespace GModule.Internal;

public partial class ModuleHandle
{
    public partial ModuleOwnedHandle OwnedCopy()
    {
        throw new NotSupportedException($"Can't create a copy of a {nameof(ModuleHandle)}.");
    }

    public partial ModuleUnownedHandle UnownedCopy()
    {
        throw new NotSupportedException($"Can't create a copy of a {nameof(ModuleHandle)}.");
    }
}

public partial class ModuleOwnedHandle
{
    [DllImport(ImportResolver.Library, EntryPoint = "g_module_close")]
    private static extern bool Close(IntPtr module);

    public static partial ModuleOwnedHandle FromUnowned(IntPtr ptr)
    {
        throw new NotSupportedException($"Can't create a copy of a {nameof(ModuleHandle)}.");
    }

    protected override partial bool ReleaseHandle()
    {
        return Close(handle);
    }
}
