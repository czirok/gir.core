namespace Generator.Renderer.Internal.Field;

internal class GLibPointerArray : FieldConverter
{
    public bool Supports(GirModel.Field field)
    {
        return field.AnyTypeOrCallback.TryPickT0(out var anyType, out _) && anyType.IsGLibPtrArray();
    }

    public RenderableField[] Convert(GirModel.Field field)
    {
        return [new RenderableField(
            Name: Model.Field.GetName(field),
            Attribute: null,
            NullableTypeName: Model.PointerArrayType.GetFullyQuallifiedHandle()
        )];
    }
}
