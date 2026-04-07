# Elemzés

## Röviden

A hibák túlnyomó többsége ugyanarra a generátorhibára vezethető vissza:

- a Gck GIR-ben valóban létezik `handle` nevű property,
- a public property-generátor ezt gondolkodás nélkül `Handle` néven generálja le,
- ez eltakarja az örökölt natív `Handle` tagot,
- a public metódusgenerátor viszont továbbra is azt feltételezi, hogy a `.Handle` egy natív safe handle, amin lehet `DangerousGetHandle()`-t hívni.

Ennek eredménye, hogy a fordító a `.Handle` alatt már nem a natív handle objektumot, hanem a generált `ulong Handle` property-t látja, és ezért keletkezik a:

- `ulong` does not contain a definition for `DangerousGetHandle`

hiba újra és újra.

## A konkrét bizonyíték a generált kódban

Az alábbi public fájlokban létrejön egy tényleges, publikus `Handle` property `ulong` típussal:

- [`src/Libs/Gck-2/Public/Object.Properties.Generated.cs`](/src/Libs/Gck-2/Public/Object.Properties.Generated.cs)
- [`src/Libs/Gck-2/Public/Session.Properties.Generated.cs`](/src/Libs/Gck-2/Public/Session.Properties.Generated.cs)
- [`src/Libs/Gck-2/Public/Slot.Properties.Generated.cs`](/src/Libs/Gck-2/Public/Slot.Properties.Generated.cs)

Példa:

- `public ulong Handle`

Ez a property nem a natív objektum-handle, hanem a GObject property-rendszeren át elért PKCS#11 azonosító.

Közben a public metódusok és paraméterkonverziók ilyen kódot generálnak:

- `slot.Handle.DangerousGetHandle()`
- `session.Handle.DangerousGetHandle()`
- `key.Handle.DangerousGetHandle()`
- `wrapper.Handle.DangerousGetHandle()`
- `this.Handle.DangerousGetHandle()`

Amint a `Handle` név egy `ulong` property-re mutat, ezek a sorok hibásak lesznek.

## A hiba forrása a GIR-ben nem hamis adat

A GIR-ben tényleg vannak ilyen property-k:

- `Gck.Object.handle`
- `Gck.Session.handle`
- `Gck.Slot.handle`

Mindegyik `gulong` típusú, vagyis a generátor nem kitalál valamit, hanem valós introspection adatból dolgozik.

A probléma tehát nem az, hogy a GIR rossz, hanem az, hogy a public névadási és ütközéskezelési logika nem számol az öröklött `Handle` taggal.

## Pontosan hol hibázik a generátor

### 1. A property neve gondolkodás nélkül `Handle` lesz

Fájl:

- [`src/Generation/Generator/Model/Property.cs`](/src/Generation/Generator/Model/Property.cs)

Itt a property neve egyszerűen PascalCase alakra megy át:

- `handle` -> `Handle`

Itt nincs olyan védelem, amely megvizsgálná, hogy ez a név ütközik-e egy örökölt taggal.

### 2. A meglévő fixerek nem ezt az esetet kezelik

Meglévő fixerek:

- [`src/Generation/Generator/Fixer/Class/PropertyNamedLikeClassFixer.cs`](/src/Generation/Generator/Fixer/Class/PropertyNamedLikeClassFixer.cs)
- [`src/Generation/Generator/Fixer/Class/PublicMethodsColldingWithPropertiesFixer.cs`](/src/Generation/Generator/Fixer/Class/PublicMethodsColldingWithPropertiesFixer.cs)
- [`src/Generation/Generator/Fixer/Class/PropertyLikeInterfacePropertyFixer.cs`](/src/Generation/Generator/Fixer/Class/PropertyLikeInterfacePropertyFixer.cs)

Ezek kezelnek bizonyos névütközéseket, de nem kezelik azt az esetet, amikor egy property neve ütközik a bázisosztályból örökölt taggal.

Vagyis jelenleg van névütközés-kezelés, csak éppen nem arra az esetre, ami itt előállt.

### 3. A public paraméter- és példánykonverziók safe handle-t feltételeznek

Fájlok:

- [`src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/Class.cs`](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/Class.cs)
- [`src/Generation/Generator/Renderer/Public/InstanceParameterToNativeExpression/Converter/Class.cs`](/src/Generation/Generator/Renderer/Public/InstanceParameterToNativeExpression/Converter/Class.cs)

Ezek a generátorban abból indulnak ki, hogy egy osztálypéldány `.Handle` tagja natív handle objektum, ezért ilyen kódot gyártanak:

- `parameter.Handle.DangerousGetHandle()`
- `this.Handle.DangerousGetHandle()`
- `base.Handle.DangerousGetHandle()`

Ez az elv helyes lenne, ha a class public felületén a `Handle` név nem lenne eltakarva egy `ulong` property által.

## Miért lesz ettől ennyi hiba egyszerre?

Mert ugyanaz a hibás névütközés több generált public fájlt is érint:

- static factory és helper metódusok
- példánymetódusok
- interfészimplementációk
- leszármazott osztályok, például `ObjectCacheHelper`

Ezért sok különböző helyen jelenik meg ugyanaz a fordítási minta, de a gyökérok egyetlen közös generátorhiba.

## A külön mellékhiba az `Object.Framework.Generated.cs` fájlban

Ebben a fájlban nem csak a `Handle` névütközés látszik, hanem egy külön lokális névütközés is:

- a `FromHandle(Gck.Session session, ulong objectHandle)` paraméter neve `objectHandle`
- a metódustörzsben a generátor ugyanilyen néven akar lokális változót létrehozni:
  - `var objectHandle = ...`

Ez okozza a következő hibákat:

- `CS0136`
- `CS0841`

Ez nem ugyanaz a hiba, mint a `DangerousGetHandle`-probléma, de ugyanabban a generált területben jelenik meg.

## Következtetés

A Gck esetben a fő generátorhiba ez:

- a public property-generálás létrehoz egy `Handle` nevű `ulong` property-t,
- de a public metódusgenerálás közben a rendszer továbbra is az örökölt natív handle-re számít ugyanazon a néven.

Vagyis a generátor két külön része ellentmond egymásnak:

1. az egyik `Handle` néven publikus PKCS#11 azonosítót generál,
2. a másik ugyanazon a néven natív safe handle-t vár.

Ezért a generált kód típusrendszerileg önellentmondó lesz.

## Milyen generátorjavítás kell ide?

Ennél a hibánál a filozófiához legjobban illő javítás nem a GIR módosítása, hanem az ütköző property-név kezelése a generátorban.

Szükséges irány:

- a property-generálásnál fel kell ismerni, ha a `Handle` név ütközik örökölt taggal,
- ilyen esetben a property-t át kell nevezni, vagy ki kell zárni a public felületről,
- és a property-descriptor generálásnak is ugyanazt az új nevet kell követnie.

Röviden: itt nem a `ulong` konverzió a fő hiba, hanem a `Handle` név ütközése a public API-ban.
