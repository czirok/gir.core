using System;

namespace Generator.Renderer.Internal.Field;

internal class OpaqueTypedRecordArray : FieldConverter
{
    public bool Supports(GirModel.Field field)
    {
        return field.AnyTypeOrCallback.TryPickT0(out var anyType, out _)
                && anyType.IsArray<GirModel.Record>(out var record)
                && Model.Record.IsOpaqueTyped(record);
    }

    public RenderableField[] Convert(GirModel.Field field)
    {
        var arrayType = field.AnyTypeOrCallback.AsT0.AsT1;
        var record = (GirModel.Record)arrayType.AnyType.AsT0;

        if (!arrayType.IsPointer)
            throw new Exception($"Unpointed opaque record array of type {record.Name} not yet supported");

        return [new RenderableField(
            Name: Model.Field.GetName(field),
            Attribute: GetAttribute(arrayType),
            NullableTypeName: Model.Type.Pointer + "[]"
        )];
    }

    private static string? GetAttribute(GirModel.ArrayType arrayType)
    {
        return arrayType.FixedSize is not null
            ? MarshalAs.UnmanagedByValArray(sizeConst: arrayType.FixedSize.Value)
            : null;
    }
}
