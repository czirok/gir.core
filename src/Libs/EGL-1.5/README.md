# Manual generated EGL-1.5.gir

> [!CAUTION]
> GNOME 50 required.

## Ubuntu 26.04

```bash
sudo apt install libglvnd-dev
```

## Arch Linux

```bash
sudo pacman -S libglvnd
```

```bash
g-ir-scanner \
  --warn-all \
  --namespace=EGL \
  --nsversion=1.5 \
  --output=EGL-1.5.gir \
  --library=EGL \
  --identifier-prefix=PFNEGL \
  --identifier-prefix=_egl \
  --identifier-filter-cmd='cat' \
  --symbol-prefix=egl \
  --function-decoration=EGLAPI \
  --accept-unprefixed \
  --cflags-begin \
    -DEGL_NO_PLATFORM_SPECIFIC_TYPES \
    -DNativeDisplayType=EGLNativeDisplayType_UNUSED \
    -DNativePixmapType=EGLNativePixmapType_UNUSED \
    -DNativeWindowType=EGLNativeWindowType_UNUSED \
    -Dkhronos_int8_t=int8_t \
    -Dkhronos_uint8_t=uint8_t \
    -Dkhronos_int16_t=int16_t \
    -Dkhronos_uint16_t=int16_t \
    -Dkhronos_int32_t=int32_t \
    -Dkhronos_uint32_t=uint32_t \
    -Dkhronos_int64_t=int64_t \
    -Dkhronos_uint64_t=uint64_t \
    -Dkhronos_intptr_t=intptr_t \
    -Dkhronos_uintptr_t=uintptr_t \
    -Dkhronos_ssize_t=ssize_t \
    -Dkhronos_usize_t=size_t \
    -Dkhronos_float_t=float \
    -Dkhronos_time_ns_t=uint64_t \
    -Dkhronos_utime_nanoseconds_t=uint64_t \
    -Dkhronos_stime_nanoseconds_t=int64_t \
    -Dkhronos_boolean_enum_t=int \
    -D__eglMustCastToProperFunctionPointerType='void *' \
  --cflags-end \
  -I/usr/include \
  -I/usr/include/EGL \
  -I/usr/include/KHR \
  /usr/include/EGL/eglplatform.h \
  /usr/include/EGL/egl.h \
  /usr/include/EGL/eglext.h
```
