namespace Generator.Renderer.Internal.Field;

internal class UntypedRecordArray : FieldConverter
{
    public bool Supports(GirModel.Field field)
    {
        return field.AnyTypeOrCallback.TryPickT0(out var anyType, out _) && anyType.IsArray<GirModel.Record>(out var record) && Model.Record.IsUntyped(record); ;
    }

    public RenderableField Convert(GirModel.Field field)
    {
        return new RenderableField(
            Name: Model.Field.GetName(field),
            Attribute: GetAttribute(field),
            NullableTypeName: GetNullableTypeName(field)
        );
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
        var type = (GirModel.Record) arrayType.AnyType.AsT0;
        return Model.UntypedRecord.GetFullyQuallifiedDataName(type) + "[]";
    }
}
