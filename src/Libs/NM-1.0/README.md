# NM-1.0.gir patch

## Add `introspectable="0"` to callback definition

`<callback name="UtilsCheckFilePredicate" c:type="NMUtilsCheckFilePredicate" introspectable="0" throws="1">`

## Replace

all from:

```xml
<type name="UtilsCheckFilePredicate" c:type="NMUtilsCheckFilePredicate" />
```

to:

```xml
<type name="gpointer" c:type="gpointer" />
```
