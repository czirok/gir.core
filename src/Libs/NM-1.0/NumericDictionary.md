# Numeric Prefix Dictionary in the Generator

This dictionary is needed because some GIR type names start with digits, and the current generic fallback transformation produces names that are linguistically valid but professionally inaccurate or misleading.

Example:

- Input: 80211ApFlags
- Generic fallback result: Eight0211ApFlags
- Expected domain-specific result: IEEE80211ApFlags

## Why it is needed

- In domains like Wi-Fi, certain numeric prefixes have established professional forms.
- The `80211` prefix is typically represented as `IEEE80211` according to industry standards.
- The generic digit-to-word conversion does not capture this domain meaning.

## What it does

- `KnownNumericPrefixes` is a mapping of numeric prefixes to semantic prefixes.
- If an identifier starts with digits, this dictionary is checked first.
- If a matching prefix is found (e.g. `80211`), it is replaced with the mapped value (`IEEE80211`), while the rest of the name remains unchanged.
- If no match is found, the existing generic fallback logic is used.

## Advantages

- Produces more readable and professionally accurate generated API names.
- Does not require replacing the existing generic conversion logic, only selectively overriding known cases.
- Easily extensible: new special prefixes can be added to the dictionary.
- More maintainable than handling special cases with scattered conditional logic.

## In short

This is a domain-specific exception dictionary for identifier escaping: it maps known numeric patterns to standardized names, while leaving all other cases to the existing fallback behavior.
