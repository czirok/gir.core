using System;

#nullable enable

namespace Atk.Internal;

public partial struct DocumentIfaceData
{
    public delegate IntPtr GetDocumentAttributesCallback(IntPtr document);
    public delegate IntPtr GetTextSelectionsCallback(IntPtr document);
    public delegate bool SetTextSelectionsCallback(IntPtr document, IntPtr selections);
}
