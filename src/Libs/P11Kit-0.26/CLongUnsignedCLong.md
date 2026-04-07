# Fix for the CLong and UnsignedCLong Pointer Field Converters

## Why was the generator modification necessary?

The issue was caused by a type-handling inconsistency in the generated public C# field properties.

The affected C fields are pointer types in the original source:

- `unsigned long* password_len`
- `unsigned long* output_len_ptr`

From GIR, these are correctly represented as pointers, and in the internal layer they are generated as `IntPtr` fields.
However, in the public layer, the `CLong` and `UnsignedCLong` converters still behaved as if these were non-pointer fields.

This resulted in two incorrect code patterns:

- getter: `Handle.GetX().Value`
- setter: `Handle.SetX(new CLong(...))` or `Handle.SetX(new CULong(...))`

For pointer fields, `Handle.GetX()` returns `nint`/`IntPtr`, which does not have a `Value` property, and the setter expects `IntPtr`, not `CLong`/`CULong`.

This led to the following compilation errors:

- `nint` does not contain a definition for `Value`
- `CULong` cannot be converted to `nint`

## What was changed in the generator?

The key change ensures that the `public` `CLong` and `UnsignedCLong` field converters are only applied to non-pointer fields.

Modified files:

- [src/Generation/Generator/Renderer/Public/Field/Converter/CLong.cs](/src/Generation/Generator/Renderer/Public/Field/Converter/CLong.cs)
- [src/Generation/Generator/Renderer/Public/Field/Converter/UnsignedCLong.cs](/src/Generation/Generator/Renderer/Public/Field/Converter/UnsignedCLong.cs)

A condition was added to the `Supports` method:

- the converter is only supported if the field is not a pointer

Practical consequences:

1. Non-pointer `CLong` and `UnsignedCLong` fields retain their specialized wrap/unwrap logic.
2. Pointer `CLong` and `UnsignedCLong` fields fall back to the generic `PrimitiveValueType` path.
3. The generic path generates direct `IntPtr`-based get/set code, which matches the internal handle signatures.

## Why does this fit the generator philosophy?

This solution maintains a clean separation of responsibilities:

- the specialized converter handles only truly special, non-pointer `CLong` or `UnsignedCLong` cases
- pointer handling remains in the general primitive path

This reduces duplicated logic, lowers regression risk, and keeps the type system consistent between the `public` and `internal` layers.

## What errors does this fix?

It directly eliminates compilation errors that occur when `Value` access or `CLong`/`CULong` setter conversions are generated for pointer-based fields.

Notable affected examples:

- [src/Libs/P11Kit-0.26/Internal/CK_PKCS5_PBKD2_PARAMSData.Generated.cs](/src/Libs/P11Kit-0.26/Internal/CK_PKCS5_PBKD2_PARAMSData.Generated.cs)
- [src/Libs/P11Kit-0.26/Internal/CK_TLS_PRF_PARAMSData.Generated.cs](/src/Libs/P11Kit-0.26/Internal/CK_TLS_PRF_PARAMSData.Generated.cs)
- [src/Libs/P11Kit-0.26/Internal/CK_WTLS_PRF_PARAMSData.Generated.cs](/src/Libs/P11Kit-0.26/Internal/CK_WTLS_PRF_PARAMSData.Generated.cs)
- [src/Libs/P11Kit-0.26/Public/CK_PKCS5_PBKD2_PARAMS.Generated.cs](/src/Libs/P11Kit-0.26/Public/CK_PKCS5_PBKD2_PARAMS.Generated.cs)
- [src/Libs/P11Kit-0.26/Public/CK_TLS_PRF_PARAMS.Generated.cs](/src/Libs/P11Kit-0.26/Public/CK_TLS_PRF_PARAMS.Generated.cs)
- [src/Libs/P11Kit-0.26/Public/CK_WTLS_PRF_PARAMS.Generated.cs](/src/Libs/P11Kit-0.26/Public/CK_WTLS_PRF_PARAMS.Generated.cs)

For these fields, the generated `public` properties will now produce `IntPtr`-compatible get/set code.

## Second round: constructor name collision and array length conversion

### CS0136 / CS0841 — local variable name collision in constructor

**Problem:** The constructor generator created a local variable named `{class.Name.ToLower()}Handle` for the return value of an internal call. If the constructor already had a parameter with the same name (for example `Object.FromHandle(Session session, ulong objectHandle)`), the compiler raised `CS0136`/`CS0841` because the same identifier was used twice in the same scope.

**Fix:** The local variable name was changed to follow the pattern `result{ClassName}Handle`, ensuring it cannot collide with parameter names.

Modified file:

- [src/Generation/Generator/Renderer/Public/Constructor/ConstructorRenderer.cs](../../Generation/Generator/Renderer/Public/Constructor/ConstructorRenderer.cs)

```csharp
// before
var variableName = $"{constructor.Parent.Name.ToLower()}Handle";

// after
var variableName = $"result{constructor.Parent.Name}Handle";
```

### CS1503 — `ulong` cannot be converted to `CULong` for array length parameters

**Problem:** In some GIR function signatures, the array length parameter type is `CULong` (for example `gck_objects_from_handle_array`, parameter `nObjectHandles`). Previously, array converters uniformly generated `(ulong) x.Length`, which caused a `CS1503` compilation error when a `CULong` was expected.

**Fix:** A new centralized helper class was introduced to generate the correct length expression based on the actual parameter type:

- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/ArrayLengthConversion.cs](../../Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/ArrayLengthConversion.cs)

```csharp
internal static class ArrayLengthConversion
{
    public static string RenderValue(ParameterToNativeData lengthParameter, string valueExpression)
    {
        var type = lengthParameter.Parameter.AnyTypeOrVarArgs.AsT0.AsT0;
        return type switch
        {
            GirModel.CLong         => $"new CLong(checked((nint){valueExpression}))",
            GirModel.UnsignedCLong => $"new CULong(checked((nuint){valueExpression}))",
            _                      => $"({Model.Type.GetName(type)}) {valueExpression}"
        };
    }
}
```

All affected array converters were updated to use this helper:

- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/PrimitiveValueTypeArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/PrimitiveValueTypeArray.cs)
- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/TypedRecordArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/TypedRecordArray.cs)
- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/UntypedRecordArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/UntypedRecordArray.cs)
- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/OpaqueTypedRecordArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/OpaqueTypedRecordArray.cs)
- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/OpaqueUntypedRecordArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/OpaqueUntypedRecordArray.cs)
- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/ForeignTypedRecordArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/ForeignTypedRecordArray.cs)
- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/Utf8StringArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/Utf8StringArray.cs)
- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/PlatformStringArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/PlatformStringArray.cs)

As a result, all array length conversions are now type-correct, regardless of whether the parameter is based on `int`/`uint` or `CLong`/`CULong`.
