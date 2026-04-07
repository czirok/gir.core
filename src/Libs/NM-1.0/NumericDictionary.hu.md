# Numeric Prefix Szótár A Generátorban

Ez a szótár azért kell, mert vannak olyan GIR típusnevek, amelyek számmal kezdődnek, és a jelenlegi általános fallback átalakítás ezekre nyelvileg helyes, de szakmailag csúnya vagy félrevezető nevet ad.

Példa:

- Bemenet: 80211ApFlags
- Általános fallback eredmény: Eight0211ApFlags
- Elvárt, domain-specifikus eredmény: IEEE80211ApFlags

## Miért kell

- A Wi-Fi és hasonló területeken bizonyos numerikus prefixeknek bevett szakmai alakjuk van.
- A 80211 prefix ipari szabvány szerint tipikusan IEEE80211 formában jelenik meg.
- Az általános számjegy->szó konverzió nem ismeri ezt a domain-jelentést.

## Mit csinál

- A KnownNumericPrefixes egy numerikus prefix -> szemantikus prefix leképezés.
- Ha az azonosító számmal kezdődik, először ez a szótár kerül ellenőrzésre.
- Ha talál egyező prefixet (például 80211), akkor azt a megadott értékre cseréli (IEEE80211), és a név többi része változatlan marad.
- Ha nincs egyezés, akkor marad a meglévő általános fallback logika (számjegy szóalakos kezelése).

## Előnyei

- Olvashatóbb és szakmailag helyesebb generált API-nevek.
- Nem kell a meglévő általános konverziós logikát kidobni, csak célzottan felülírni az ismert kivételeket.
- Könnyen bővíthető: új speciális prefix esetén elég a szótárba új bejegyzést tenni.
- Karbantarthatóbb megoldás, mint fix, szétszórt if-ágakkal kezelni az egyedi eseteket.

## Röviden

Ez egy domain-specifikus kivételszótár az azonosító-escape logikához: az ismert numerikus mintákat szebb, szabványos nevekre fordítja, minden más esetben pedig meghagyja a jelenlegi fallback működést.
