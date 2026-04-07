# CLong és UnsignedCLong pointer mezők konverterének javítása

## Miért kellett a generátor módosítása?

A hiba oka egy típuskezelési ellentmondás volt a generált publikus C# mezőproperty-kben.

Az érintett C mezők a forrásban pointer típusúak:

- `unsigned long* password_len`
- `unsigned long* output_len_ptr`

A GIR-ből ez helyesen pointerként jön át, és az internal rétegben ennek megfelelően IntPtr mező generálódik.
Ennek ellenére a public réteg CLong és UnsignedCLong konvertere pointer esetben is úgy viselkedett, mintha nem pointer mező lenne.

Ez konkrétan két hibás kódsablont jelentett:

- getter: `Handle.GetX().Value`
- setter: `Handle.SetX(new CLong(...))` vagy `Handle.SetX(new CULong(...))`

Pointer mezőnél viszont a `Handle.GetX()` visszatérési típusa `nint`/`IntPtr`, amin nincs `Value` tulajdonság, és a setter is `IntPtr`-t vár, nem `CLong`/`CULong` értéket.

Ezért jelent meg fordításkor a következő hibacsoport:

- `nint` nem tartalmaz `Value` definíciót
- `CULong` nem konvertálható `nint` típusra

## Mit módosítottunk a generátorban?

A módosítás lényege, hogy a `public` `CLong` és `UnsignedCLong` mezőkonverter csak nem pointer mezőkre aktiválódjon.

Módosított fájlok:

- [src/Generation/Generator/Renderer/Public/Field/Converter/CLong.cs](/src/Generation/Generator/Renderer/Public/Field/Converter/CLong.cs)
- [src/Generation/Generator/Renderer/Public/Field/Converter/UnsignedCLong.cs](/src/Generation/Generator/Renderer/Public/Field/Converter/UnsignedCLong.cs)

A Supports függvényben bekerült a feltétel:

- csak akkor támogatott a konverter, ha a mező nem pointer

Gyakorlati következmény:

1. Nem pointer `CLong` és `UnsignedCLong` mezőknél változatlan marad a speciális wrap/unwrap logika.
2. Pointer `CLong` és `UnsignedCLong` mezőknél a konverzió visszaesik a generikus `PrimitiveValueType` ágra.
3. A generikus ág pointer esetben közvetlen `IntPtr` alapú get/set kódot ad, ami típushelyes az internal handle szignatúrákhoz.

## Miért illeszkedik ez a generátor filozófiájába?

Ez a megoldás a felelősségek tiszta szétválasztását követi:

- a speciális konverter csak a valóban speciális, nem pointer `CLong` vagy `UnsignedCLong` esetre felel
- a pointerkezelés az általános primitive útvonalon marad

Így kevesebb a duplikált logika, kisebb a regresszió kockázata, és konzisztens marad a `public` és `internal` réteg típusrendszere.

## Milyen hibákat szüntet meg a módosítás?

Közvetlenül azokat a fordítási hibákat szünteti meg, amelyek akkor jelentkeznek, amikor pointerként reprezentált mezőre Value hívás vagy CLong/CULong setter-konverzió generálódik.

Kiemelten érintett példák:

- [src/Libs/P11Kit-0.26/Internal/CK_PKCS5_PBKD2_PARAMSData.Generated.cs](/src/Libs/P11Kit-0.26/Internal/CK_PKCS5_PBKD2_PARAMSData.Generated.cs)
- [src/Libs/P11Kit-0.26/Internal/CK_TLS_PRF_PARAMSData.Generated.cs](/src/Libs/P11Kit-0.26/Internal/CK_TLS_PRF_PARAMSData.Generated.cs)
- [src/Libs/P11Kit-0.26/Internal/CK_WTLS_PRF_PARAMSData.Generated.cs](/src/Libs/P11Kit-0.26/Internal/CK_WTLS_PRF_PARAMSData.Generated.cs)
- [src/Libs/P11Kit-0.26/Public/CK_PKCS5_PBKD2_PARAMS.Generated.cs](/src/Libs/P11Kit-0.26/Public/CK_PKCS5_PBKD2_PARAMS.Generated.cs)
- [src/Libs/P11Kit-0.26/Public/CK_TLS_PRF_PARAMS.Generated.cs](/src/Libs/P11Kit-0.26/Public/CK_TLS_PRF_PARAMS.Generated.cs)
- [src/Libs/P11Kit-0.26/Public/CK_WTLS_PRF_PARAMS.Generated.cs](/src/Libs/P11Kit-0.26/Public/CK_WTLS_PRF_PARAMS.Generated.cs)

Ezeknél a mezőknél a generált `public` property ezentúl `IntPtr`-kompatibilis get/set kódot fog adni.

## Második kör: konstruktor névütközés és tömbhossz-konverzió

### CS0136 / CS0841 — lokális változó névütközés konstruktorban

**Probléma:** A konstruktorgenerátor `{class.Name.ToLower()}Handle` nevű lokális változót hozott létre a belső hívás visszatérési értékéhez. Ha az adott konstruktornak volt egy azonos nevű paramétere (pl. `Object.FromHandle(Session session, ulong objectHandle)`), akkor a fordító `CS0136`/`CS0841` hibával elutasította, mert ugyanabban a hatókörben kétszer szerepelt `objectHandle`.

**Javítás:** A lokális változó neve `result{ClassName}Handle` mintára változott, ami nem ütközhet semmilyen paraméternévvel.

Módosított fájl:

- [src/Generation/Generator/Renderer/Public/Constructor/ConstructorRenderer.cs](../../Generation/Generator/Renderer/Public/Constructor/ConstructorRenderer.cs)

```csharp
// előtte
var variableName = $"{constructor.Parent.Name.ToLower()}Handle";

// utána
var variableName = $"result{constructor.Parent.Name}Handle";
```

### CS1503 — `ulong` nem konvertálható `CULong` tömbhossz-paraméterbe

**Probléma:** Egyes GIR függvényszignatúrákban a tömbhossz-paraméter típusa `CULong` (pl. `gck_objects_from_handle_array` esetén `nObjectHandles`). Az array-konverterek korábban egységesen `(ulong) x.Length` alakú kifejezést generáltak, ami `CULong`-ot váró paraméternél `CS1503` fordítási hibát okozott.

**Javítás:** Új központi helper osztály készült, amely a paraméter tényleges típusától függően állítja elő a helyes hosszkifejezést:

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

Az összes érintett array-konverter frissítve lett erre a helperre:

- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/PrimitiveValueTypeArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/PrimitiveValueTypeArray.cs)
- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/TypedRecordArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/TypedRecordArray.cs)
- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/UntypedRecordArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/UntypedRecordArray.cs)
- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/OpaqueTypedRecordArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/OpaqueTypedRecordArray.cs)
- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/OpaqueUntypedRecordArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/OpaqueUntypedRecordArray.cs)
- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/ForeignTypedRecordArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/ForeignTypedRecordArray.cs)
- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/Utf8StringArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/Utf8StringArray.cs)
- [src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/PlatformStringArray.cs](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/PlatformStringArray.cs)

Így minden tömbhossz-konverzió típushelyes maradt, akár `int`/`uint`, akár `CLong`/`CULong` a paraméter alapja.
