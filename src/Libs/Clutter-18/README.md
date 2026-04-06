# Change Clutter-18.gir

> [!CAUTION]
> GNOME 50 required.

Remove from Clutter-18.gir:

```xml
<include name="GL" version="1.0" />
```

Add `name="guint32"` to the

- three `<type c:type="xkb_mod_mask_t*"/>`
- one `<type c:type="xkb_layout_index_t"/>`

like this:

- `<type name="guint32" c:type="xkb_mod_mask_t*"/>`
- `<type name="guint32" c:type="xkb_layout_index_t"/>`
