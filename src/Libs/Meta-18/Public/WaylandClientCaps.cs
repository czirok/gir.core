using System;

namespace Meta;

[Flags]
public enum WaylandClientCaps
{
    None = 0,
    X11Interop = 1 << 0
}
