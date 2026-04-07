using Generator.Model;

namespace Generator.Renderer.Internal.Field;

internal class ClassArray : FieldConverter
{
    public bool Supports(GirModel.Field field)
    {
        return field.AnyTypeOrCallback.TryPickT0(out var anyType, out _) && anyType.IsArray<GirModel.Class>();
    }

    public RenderableField[] Convert(GirModel.Field field)
    {
        return [new RenderableField(
            Name: Model.Field.GetName(field),
            Attribute: GetAttribute(field),
            NullableTypeName: GetNullableTypeName(field)
        )];
    }

    private static string? GetAttribute(GirModel.Field field)
    {
        var arrayType = field.AnyTypeOrCallback.AsT0.AsT1;
        return arrayType.FixedSize is not null
            ? MarshalAs.UnmanagedByValArray(sizeConst: arrayType.FixedSize.Value)
            : null;
    }

    private static string GetNullableTypeName(GirModel.Field field)
    {
        var arrayType = field.AnyTypeOrCallback.AsT0.AsT1;

        if (arrayType.AnyType.TryPickT0(out var anyType, out _) && anyType is GirModel.Class @class)
            return $"{Model.Namespace.GetPublicName(@class.Namespace)}.{Model.Type.GetName(@class)}[]";

        return ArrayType.GetName(arrayType);
    }
}
