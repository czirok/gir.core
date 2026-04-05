using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#nullable enable

namespace Atk;

public partial class NoOpObject
{
    [Version("1.3")]
    public TextRange[] GetBoundedRanges(TextRectangle rect, CoordType coordType, TextClipType xClipType, TextClipType yClipType)
    {
        ArgumentNullException.ThrowIfNull(rect, nameof(rect));

        var rangesPtr = Atk.Internal.Text.GetBoundedRanges(
            this.Handle.DangerousGetHandle(),
            rect.Handle,
            coordType,
            xClipType,
            yClipType
        );

        if (rangesPtr is null || rangesPtr.Length == 0)
            return [];

        var ranges = new List<TextRange>(rangesPtr.Length);

        foreach (var ptr in rangesPtr)
        {
            if (ptr == IntPtr.Zero)
                break;

            var ownedHandle = new Atk.Internal.TextRangeUnownedHandle(ptr).OwnedCopy();
            ranges.Add(new TextRange(ownedHandle));
        }

        return ranges.ToArray();
    }
}
