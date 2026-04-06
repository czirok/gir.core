using System;
using System.Collections.Generic;

namespace Generator.Renderer.Public.ParameterToNativeExpressions;

internal class Union : ToNativeParameterConverter
{
    public bool Supports(GirModel.AnyType type)
        => type.Is<GirModel.Union>(out var union) && union.TypeFunction is not null;

    public void Initialize(ParameterToNativeData parameter, IEnumerable<ParameterToNativeData> _)
    {
        if (parameter.Parameter.Direction != GirModel.Direction.In)
            throw new NotImplementedException($"{parameter.Parameter.AnyTypeOrVarArgs}: union parameter with direction != in not yet supported");

        if (!parameter.Parameter.IsPointer)
            throw new NotImplementedException($"{parameter.Parameter.AnyTypeOrVarArgs}: union parameter which is no pointer can not be converted to native");

        var parameterName = Model.Parameter.GetName(parameter.Parameter);
        var callParameter = parameter.Parameter.Nullable
            ? parameterName + "?.Handle ?? IntPtr.Zero"
            : parameterName + ".Handle";

        parameter.SetSignatureName(() => parameterName);
        parameter.SetCallName(() => callParameter);
    }
}
