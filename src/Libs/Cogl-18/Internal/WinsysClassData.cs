using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

#nullable enable

namespace Cogl.Internal;

public partial struct WinsysClassData
{
    public delegate IntPtr RendererGetProcAddressCallback(IntPtr winsys, IntPtr renderer, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    public delegate IntPtr RendererQueryDrmModifiersCallback(IntPtr winsys, IntPtr renderer, Cogl.PixelFormat format, Cogl.DrmModifierFilter filter, out GLib.Internal.ErrorOwnedHandle error);
}
