using System.Collections.Generic;
using Generator.Model;

namespace Generator.Renderer.Public.ReturnTypeToManagedExpressions;

internal class Union : ReturnTypeConverter
{
    public bool Supports(GirModel.AnyType type)
        => type.Is<GirModel.Union>(out var union) && union.TypeFunction is not null;

    public void Initialize(ReturnTypeToManagedData data, IEnumerable<ParameterToNativeData> _)
    {
        data.SetExpression(fromVariableName =>
        {
            var returnType = data.ReturnType;
            var union = (GirModel.Union)returnType.AnyType.AsT0;
            var ctor = $"new {ComplexType.GetFullyQualified(union)}({fromVariableName})";

            return returnType.Nullable
                ? $"{fromVariableName} == IntPtr.Zero ? null : {ctor}"
                : ctor;
        });
    }
}
