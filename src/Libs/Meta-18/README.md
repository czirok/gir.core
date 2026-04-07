# Meta18.gir patch

Replace from:

```xml
<type c:type="MetaDBusDebugControlSkeletonClass" />
```

to:

```xml
<type name="DBusDebugControlSkeletonClass" c:type="MetaDBusDebugControlSkeletonClass" />
```

Insert this before `<record name="DebugControlClass"...`:

```xml
<record name="DBusDebugControlSkeletonClass" c:type="MetaDBusDebugControlSkeletonClass" disguised="1" opaque="1" />
```

Replace from:

```xml
<type name="X11DisplayEventFunc" c:type="MetaX11DisplayEventFunc" />
```

to:

```xml
<type name="gpointer" c:type="gpointer" />
```
