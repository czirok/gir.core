using System;

#nullable enable

namespace Atk.Internal;

public partial struct EditableTextIfaceData
{
    public delegate bool SetRunAttributesCallback(IntPtr text, IntPtr attribSet, int startOffset, int endOffset);
}
