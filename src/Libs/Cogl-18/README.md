# Change Cogl-18.gir

> [!CAUTION]
> GNOME 50 required.

Remove from Cogl-18.gir:

```xml
<include name="GL" version="1.0" />
```

Add to Cogl-18.gir:

```xml
<include name="EGL" version="1.5" />
```
