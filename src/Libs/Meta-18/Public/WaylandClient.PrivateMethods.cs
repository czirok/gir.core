using System;

#nullable enable

namespace Meta;

public partial class WaylandClient
{
    public static WaylandClient NewCreate(Context context, int pid)
    {
        ArgumentNullException.ThrowIfNull(context);

        var result = Internal.WaylandClient.NewCreate(
            context.Handle.DangerousGetHandle(),
            pid,
            out GLib.Internal.ErrorOwnedHandle error);

        if (!error.IsInvalid)
            throw new GLib.GException(error);

        return CreateInstance(result);
    }

    public int TakeClientFd()
    {
        return Internal.WaylandClient.TakeClientFd(Handle.DangerousGetHandle());
    }

    public void SetCaps(WaylandClientCaps caps)
    {
        Internal.WaylandClient.SetCaps(Handle.DangerousGetHandle(), (int)caps);
    }
}
