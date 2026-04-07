namespace Generator.Renderer.Public.ParameterToNativeExpressions;

internal static class ArrayLengthConversion
{
    public static string GetNativeTypeName(ParameterToNativeData lengthParameter)
    {
        var type = lengthParameter.Parameter.AnyTypeOrVarArgs.AsT0.AsT0;

        return type switch
        {
            GirModel.CLong => "CLong",
            GirModel.UnsignedCLong => "CULong",
            _ => Model.Type.GetName(type)
        };
    }

    public static string RenderValue(ParameterToNativeData lengthParameter, string valueExpression)
    {
        var type = lengthParameter.Parameter.AnyTypeOrVarArgs.AsT0.AsT0;

        return type switch
        {
            GirModel.CLong => $"new CLong(checked((nint){valueExpression}))",
            GirModel.UnsignedCLong => $"new CULong(checked((nuint){valueExpression}))",
            _ => $"({Model.Type.GetName(type)}) {valueExpression}"
        };
    }
}
