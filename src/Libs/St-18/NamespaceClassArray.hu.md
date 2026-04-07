# Miért kellett a minimális patch a ClassArray.cs fájlban?

## A probléma

Az `St-18.gir` fájlban a `ThemeNodePaintStateData` struct egy `Cogl.Pipeline[]` tömb mezőt tartalmaz.

- **GIR forrás** (`St-18.gir` sor 10863): `<type name="Cogl.Pipeline" c:type="CoglPipeline*"/>`
- **Generált Data struct** (`ThemeNodePaintStateData.Generated.cs` sor 37): `public Cogl.Pipeline[] CornerPipeline;` **HELYES**
- **Generált Handle fájl** (`ThemeNodePaintStateHandle.Generated.cs` sor 174, 181): `public Pipeline[] GetCornerPipeline()` **HIBÁS**

A Handle fájlban hiányzott a `Cogl.` namespace előtag!

## Miért történt ez?

A generátor a `ClassArray.cs` file converterben ezt a kódot használta:

```csharp
return ArrayType.GetName(arrayType);
```

Ez a metódus csak az elem típus nevét adta vissza (`Pipeline`), de **NEM az annak a namespace-ét** (`Cogl`).

### Az ArrayType.GetName() logikája

Amikor `Cogl.Pipeline[]` típussal előtálkozik:

1. Extraktálja az elem típust: `Pipeline` (Class)
2. Meghívja `Model.Type.GetName(@class)`
3. Ez csak az unqualified nevet adja vissza: `Pipeline`
4. Az eredmény: `Pipeline[]` - **hiányzik a namespace!**

## Miért pont a ClassArray.cs?

Mert ez az osztály felelős azért, hogy a **Class típusú tömbök** (mint `Cogl.Pipeline[]`) hogyan jelenjenek meg a getter/setter metódusok aláírásaiban:

```csharp
public Pipeline[] GetCornerPipeline()    // ← Itt használódik a GetNullableTypeName() kimenetele
public void SetCornerPipeline(Pipeline[] value)
```

## A megoldás: Minimális patch

Hozzáadtunk egy egyszerű ellenőrzést a `GetNullableTypeName()` metódusban:

```csharp
if (arrayType.AnyType.TryPickT0(out var anyType, out _) && anyType is GirModel.Class @class)
    return $"{Model.Namespace.GetPublicName(@class.Namespace)}.{Model.Type.GetName(@class)}[]";

return ArrayType.GetName(arrayType);
```

### Hogyan működik

1. **Ellenőrizze**: Az elem típus Class-e?
2. **Ha igen**: Szerezze meg a namespace-et (`@class.Namespace`) és konvertálja PascalCase-re (`GetPublicName()`)
3. **Eredmény**: `"Cogl" + "." + "Pipeline" + "[]"` = `"Cogl.Pipeline[]"`
4. **Ha nem**: Használja az eredeti logikát (nem-Class típusokhoz)

## Miért nem nagyobb megoldás?

Elvetettünk egy összetettebb megközelítést (automatic `using` direktíva injektálás a Handle template-ben), mert:

- **Nem szükséges**: A probléma lokális (csak ez a converter), nem globális
- **Túl bonyolult**: Egy új infrastruktúrát igényelt volna az `using` direktívák automatikus kezeléséhez
- **Közvetlen fix**: A ClassArray converter pontosan a hely, ahol a típus név kiválasztódik

## Miért biztonságos?

- **Fallback**: Ha az elem nem Class, az eredeti `ArrayType.GetName()` metódust használja
- **Konzisztens**: A `TypedRecordArray.cs` converter már így csinálja: `GetFullyQuallifiedDataName() + "[]"`
- **Izolált**: Csak a `ClassArray` converterben módosultak 4 sor kód

## Összefoglalva

**4 soros patch** = A Class array típusok most egyértelműen qualified neveket kapnak, így:

- `Pipeline[]` -> `Cogl.Pipeline[]`

Ez a generátor output-jában automatikusan helyesre javítja az issue-t, anélkül hogy új komplexitást vagy extra infrastruktúrát kellene fenn tartani.
