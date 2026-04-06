using Generator.Model;

namespace Generator.Renderer.Public.ReturnType;

internal class Union : ReturnTypeConverter
{
    public RenderableReturnType Create(GirModel.ReturnType returnType)
    {
        var nullableTypeName = ComplexType.GetFullyQualified((GirModel.Union)returnType.AnyType.AsT0) + Nullable.Render(returnType);

        return new RenderableReturnType(nullableTypeName);
    }

    public bool Supports(GirModel.ReturnType returnType)
        => returnType.AnyType.Is<GirModel.Union>(out var union) && union.TypeFunction is not null;
}
