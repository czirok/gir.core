# Union a gir.core-ban

## Mi az a union?

A `union` a C nyelvből ismert adattípus, ahol több mező ugyanazt a memóriaterületet osztja meg. Ez azt jelenti, hogy a union egyszerre csak egy ténylegesen értelmezett reprezentációt hordoz, de ugyanazt a memóriablokkot többféle nézetben lehet olvasni.

GObject Introspection oldalon a `union` külön modell-elem, ugyanúgy, mint a `class`, `record`, `interface`, `enumeration` vagy `callback`. A GIR-ben a union tipikusan olyan natív típusokat ír le, amelyeknél a memóriaelrendezés fontosabb, mint az objektumorientált viselkedés.

## Mi a union szerepe ebben a repositoryban?

A gir.core generator feladata, hogy a GIR leírásból C# API-t készítsen. Ebben a folyamatban a union szerepe az, hogy bizonyos natív típusokhoz biztonságosan használható managed wrapper készüljön.

Ez különösen fontos olyan callbackeknél és függvényparamétereknél, ahol a GIR union típust ad meg. Ha a generator nem tud uniont kezelni, akkor:

- a publikus delegate nem generálódik le rendesen,
- a paraméter- és visszatérési konverzió megszakad,
- a generált publikus API hiányos lesz,
- a build később egy látszólag másik ponton fog elhasalni.

Erre konkrét példa volt a `Clutter.Event`, ami GIR szinten union, és emiatt a `Clutter.EventFilterFunc` publikus delegate generálása korábban üres fájlba futott.

## Hol jelenik meg a generatorban?

A union támogatás több rétegben jelenik meg:

1. Modellszint

   A GIR loader és a közös GIR modell külön union típusként kezeli az ilyen elemeket.

2. Generálási belépési pont

   A generator union-specifikus fájlokat is előállíthat, ugyanúgy, mint recordoknál vagy classoknál.

3. Public wrapper generálás

   Ha a union publikus oldalon is használható, akkor készül hozzá C# wrapper, amely egy natív handle köré épül.

4. Marshalling

   A paraméterek, visszatérési értékek és callbackek konverziójánál a generatornak tudnia kell, hogyan lesz egy natív unionból managed érték, és fordítva.

## A jelenlegi megközelítés a gir.core-ban

Jelenleg a union támogatás szándékosan szűkített.

Nem minden GIR unionból készül publikus wrapper. A publikus union generálás csak azoknál a unionöknél engedett, amelyek rendelkeznek `TypeFunction` adattal, vagyis van stabil GType-alapú azonosításuk.

Ennek oka, hogy a túl tág union támogatás hamis vagy használhatatlan publikus API-kat hozhat létre. A korábbi szélesebb megközelítés regressziókat okozott például GLib és GObject oldalon, mert olyan unionök is publikus C# típussá váltak, amelyekhez a környező infrastruktúra nem volt teljes.

## Public union wrapper

A publikus union wrapper szerepe:

- natív handle tárolása,
- egységes managed belépési pont biztosítása,
- használhatóság callbackekben és publikus metódusaláírásokban,
- GType-alapú integráció támogatása, ha az adott union ezt lehetővé teszi.

A jelenlegi megoldásban a publikus union wrapper saját `IntPtr Handle` értéket tart fenn. Nem `GObject.Fundamental` leszármazásra épül, mert ez túl erős és sok unionra hibás feltételezés lenne.

Ez a döntés azért fontos, mert a union általában nem `GObject.Object`, és nem is biztos, hogy olyan életciklus- vagy példányosítási szabályai vannak, mint egy klasszikus GObject-alapú típusnak.

## Internal és public oldal

Unionnál is két külön nézőpont van:

- Internal oldal: P/Invoke, natív hívások, alacsony szintű reprezentáció.
- Public oldal: C#-ból kényelmesen használható wrapper és típuskonverzió.

Az internal oldal önmagában még nem elég. Ha csak internal reprezentáció van, de public wrapper nincs, akkor egy publikus delegate vagy metódusaláírás könnyen generálhatatlan marad.

Pont ez történt a Clutter esetében is: az internal callback létezett, de a public callback generálása a union hiánya miatt elhasalt.

## Callbackek és union

Union támogatás nélkül a callback-renderelés azért problémás, mert a callback paramétereinek publikus C# típusát is elő kell állítani. Ha egy callback például uniont vár, akkor a generatornak tudnia kell, hogy:

- mi legyen a publikus C# paramétertípus,
- hogyan készül managed wrapper a natív pointerből,
- hogyan történik a visszaalakítás, ha szükséges.

Ha ez a lánc hiányos, a callback delegate renderelője kivételbe fut, és a jelenlegi architektúrában ez könnyen üres generált fájlhoz vezethet.

## Miért nem generálunk minden unionhoz metódusokat?

A unionök metóduskezelése érzékenyebb, mint a klasszikus class vagy record eset.

A generikus public method és constructor renderelők több helyen feltételezik, hogy a callable `Parent` kapcsolata stabilan feloldott és objektumszerűen használható. A unionos iteráció során kiderült, hogy ez nem minden unionhoz tartozó callable esetén igaz, és ebből jött az `Unknown parent` típusú hiba.

Ezért a jelenlegi implementáció tudatosan konzervatív:

- a public union wrapper csak a szükséges keretet adja,
- a public union method generálás csak a biztonságosnak ítélt útra támaszkodik,
- a generikus class/constructor logika nincs automatikusan ráengedve minden unionra.

Ez nem végleges elméleti állítás, hanem egy védelmi döntés a jelenlegi generator-architektúrában.

## A Parent probléma röviden

A loader oldalon a `Parent` nem passzív adat, hanem feloldott kapcsolat. Ha nincs feloldva, a getter kivételt dob `Unknown parent` üzenettel.

Ez azért fontos, mert a unionos regresszió során olyan renderelési út nyílt meg, ahol a renderer parentre támaszkodott, de az adott callable-nál ez nem volt biztonságos.

Ettől két külön probléma keletkezett:

- a valódi ok: hibás union renderelési út,
- a másodlagos hiba: a catch ágban lévő logolás is `Parent.Name`-et olvasott, ezért maga a hibajelentés is újra kivételbe tudott futni.

Ezért lett a logolás úgy módosítva, hogy a hibás callable-t `CIdentifier ?? Name` alapján azonosítsa. Ez nem a union gyökérokát javította, hanem a diagnosztikát tette megbízhatóvá.

## Mikor helyes uniont publikus típusként kezelni?

A jelenlegi szabály alapján akkor, ha az adott unionnak van olyan típusszintű információja, amely alapján biztonságosan beilleszthető a publikus API-ba. A gyakorlatban ez most a `TypeFunction` jelenléte.

Ez jó kompromisszum a következők között:

- ne maradjon hiányos a publikus API ott, ahol a union tényleg használható,
- ne generáljunk félkész vagy félrevezető publikus wrapper-eket olyan unionökhöz, amelyekhez nincs elég stabil típusinformáció.

## Példa: Clutter.Event

A `Clutter.Event` jó példa arra, hogy union támogatás néha elengedhetetlen.

- GIR oldalon union.
- Publikus callbackben megjelenik.
- Szüksége van managed wrapperre.
- A callback-notification útvonalnak tudnia kell `IntPtr`-ből `Clutter.Event` példányt készíteni.

Ha ez hiányzik, akkor a callback delegate nem lesz helyesen generálható.

## Tipikus hibajelek, ha a union támogatás hiányos

- Üres vagy hiányos `.Generated.cs` fájl.
- Hiányzó publikus típus a generált API-ban.
- Callback delegate nem generálódik.
- `Unknown parent` jellegű kivétel renderelés közben.
- Build error olyan névvel, amely első ránézésre nem a unionhoz kötődik.

## Fontos korlátok

- A generator jelenleg nem tisztítja automatikusan a már nem generált kimeneti fájlokat.
- Ha egy korábbi iteráció több unionhoz generált publikus wrapper-eket, később pedig a szabály szűkül, akkor a régi generált fájlok bent maradhatnak.
- Ezek a stale fájlok félrevezető build hibákat okozhatnak.

Emiatt unionnal kapcsolatos generátorváltoztatás után gyakran szükség van célzott takarításra vagy újragenerálás előtti clean lépésre.

## Összefoglalás

A union a gir.core-ban nem mellékes részlet, hanem olyan GIR konstrukció, amely közvetlenül befolyásolja:

- a publikus C# API teljességét,
- a callbackek generálhatóságát,
- a marshalling működését,
- a generator robusztusságát.

A jelenlegi cél nem az, hogy minden uniont automatikusan teljes értékű publikus típussá tegyünk, hanem az, hogy a valóban szükséges és biztonságosan kezelhető unionök működjenek stabilan. Ennek jó példája a `Clutter.Event`, ahol a union támogatás hiánya közvetlenül hibás generálást okozott.

## Jelenlegi hatókör

A jelenlegi állapot alapján ez a union módosítás gyakorlati értelemben csak a Cluttert érinti.

Ennek az az oka, hogy a linuxos GIR fájlok között jelenleg csak a `Clutter.Event` union felel meg annak a feltételnek, amely alapján publikus union wrapper generálódik, és az ellenőrzött generált kimenetben is csak ez jelent meg publikus unionként.

Ezért a mostani implementáció ténylegesen a `Clutter.Event` és az arra épülő Clutter callback- és marshalling-útvonalakat javítja. Ez nem általános állítás minden jövőbeli GIR állapotra, hanem a jelenlegi repositoryállapotból következő megfigyelés.
