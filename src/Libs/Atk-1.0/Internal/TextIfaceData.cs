using System;

#nullable enable

namespace Atk.Internal;

public partial struct TextIfaceData
{
    public delegate IntPtr GetRunAttributesCallback(IntPtr text, int offset, out int startOffset, out int endOffset);
    public delegate IntPtr GetDefaultAttributesCallback(IntPtr text);
}
