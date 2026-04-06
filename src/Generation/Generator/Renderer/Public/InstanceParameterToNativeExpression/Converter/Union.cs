namespace Generator.Renderer.Public.InstanceParameterToNativeExpressions;

internal class Union : InstanceParameterConverter
{
    public bool Supports(GirModel.Type type)
    {
        return type is GirModel.Union union && union.TypeFunction is not null;
    }

    public string GetExpression(GirModel.InstanceParameter instanceParameter)
    {
        return "this.Handle";
    }
}
