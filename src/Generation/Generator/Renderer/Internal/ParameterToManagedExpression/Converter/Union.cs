using System;
using System.Collections.Generic;
using Generator.Model;

namespace Generator.Renderer.Internal.ParameterToManagedExpressions;

internal class Union : ToManagedParameterConverter
{
    public bool Supports(GirModel.AnyType type)
        => type.Is<GirModel.Union>(out var union) && union.TypeFunction is not null;

    public void Initialize(ParameterToManagedData parameterData, IEnumerable<ParameterToManagedData> parameters)
    {
        if (!parameterData.Parameter.IsPointer)
            throw new NotImplementedException($"{parameterData.Parameter.AnyTypeOrVarArgs}: union parameter which is no pointer can not be converted to managed");

        if (parameterData.Parameter.Direction != GirModel.Direction.In)
            throw new NotImplementedException($"{parameterData.Parameter.AnyTypeOrVarArgs}: union parameter with direction != in not yet supported");

        var union = (GirModel.Union)parameterData.Parameter.AnyTypeOrVarArgs.AsT0.AsT0;
        var signatureName = Model.Parameter.GetName(parameterData.Parameter);
        var variableName = Model.Parameter.GetConvertedName(parameterData.Parameter);
        var typeName = ComplexType.GetFullyQualified(union);

        var expression = parameterData.Parameter.Nullable
            ? $"var {variableName} = {signatureName} == IntPtr.Zero ? null : new {typeName}({signatureName});"
            : $"var {variableName} = new {typeName}({signatureName});";

        parameterData.SetSignatureName(() => signatureName);
        parameterData.SetExpression(() => expression);
        parameterData.SetCallName(() => variableName);
    }
}
