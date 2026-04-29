using System;
using GirModel;

namespace Generator.Renderer.Internal.ReturnTypeToNativeExpressions;

internal class Class : ReturnTypeConverter
{
    public bool Supports(AnyType type)
        => type.Is<GirModel.Class>();

    public string GetString(GirModel.ReturnType returnType, string fromVariableName)
    {
        if (!returnType.IsPointer)
            throw new NotImplementedException($"{returnType.AnyType}: class return type which is no pointer can not be converted to native");

        var isFundamental = returnType.AnyType.Is<GirModel.Class>(out var cls) && cls.Fundamental;

        if (isFundamental)
        {
            return returnType.Nullable
                ? fromVariableName + "?.Handle ?? IntPtr.Zero"
                : fromVariableName + ".Handle";
        }

        return returnType.Nullable
            ? fromVariableName + "?.Handle.DangerousGetHandle() ?? IntPtr.Zero"
            : fromVariableName + ".Handle.DangerousGetHandle()";
    }
}
