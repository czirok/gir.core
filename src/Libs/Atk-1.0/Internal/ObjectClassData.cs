using System;

#nullable enable

namespace Atk.Internal;

public partial struct ObjectClassData
{
    public delegate IntPtr GetAttributesCallback(IntPtr accessible);
}
