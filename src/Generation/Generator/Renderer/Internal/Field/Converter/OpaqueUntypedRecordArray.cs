using System;

namespace Generator.Renderer.Internal.Field;

internal class OpaqueUntypedRecordArray : FieldConverter
{
    public bool Supports(GirModel.Field field)
    {
        return field.AnyTypeOrCallback.TryPickT0(out var anyType, out _)
                && anyType.IsArray<GirModel.Record>(out var record)
                && Model.Record.IsOpaqueUntyped(record);
    }

    public RenderableField[] Convert(GirModel.Field field)
    {
        var arrayType = field.AnyTypeOrCallback.AsT0.AsT1;
        var record = (GirModel.Record)arrayType.AnyType.AsT0;

        if (!arrayType.IsPointer)
            throw new Exception($"Unpointed opaque untyped record array of type {record.Name} not yet supported");

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
