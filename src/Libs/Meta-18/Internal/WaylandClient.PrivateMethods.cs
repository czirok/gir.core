using System;
using System.Runtime.InteropServices;

#nullable enable

namespace Meta.Internal;

public partial class WaylandClient
{
    [DllImport(ImportResolver.Library, EntryPoint = "meta_wayland_client_new_create")]
    public static extern IntPtr NewCreate(
        IntPtr context,
        int pid,
        out GLib.Internal.ErrorOwnedHandle error);

    [DllImport(ImportResolver.Library, EntryPoint = "meta_wayland_client_take_client_fd")]
    public static extern int TakeClientFd(IntPtr client);

    [DllImport(ImportResolver.Library, EntryPoint = "meta_wayland_client_set_caps")]
    public static extern void SetCaps(IntPtr client, int caps);
}
