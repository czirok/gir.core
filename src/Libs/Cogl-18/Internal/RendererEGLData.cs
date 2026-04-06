
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

#nullable enable

namespace Cogl.Internal;

public partial struct RendererEGLData
{
    public delegate IntPtr PfEglCreateImageCallback(IntPtr dpy, IntPtr ctx, EGL.EGLenum target, IntPtr buffer, ref EGL.EGLint attrib_list);

    public delegate uint PfEglQueryWaylandBufferCallback(IntPtr dpy, IntPtr buffer, EGL.EGLint attribute, ref EGL.EGLint value);

    public delegate IntPtr PfEglCreateSyncCallback(IntPtr dpy, EGL.EGLenum type, ref EGL.EGLint attrib_list);
}
