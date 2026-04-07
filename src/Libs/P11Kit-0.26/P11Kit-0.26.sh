#!/bin/bash
set -e

g-ir-scanner \
  --warn-all \
  --namespace=P11Kit \
  --nsversion=0.26 \
  --output=P11Kit-0.26.gir \
  --pkg=p11-kit-1 \
  --library=p11-kit \
  --identifier-prefix= \
  --identifier-prefix=P11Kit \
  --identifier-prefix=P11KitUri \
  --identifier-prefix=p11_kit \
  --identifier-filter-cmd='cat' \
  --symbol-prefix=p11_kit \
  --accept-unprefixed \
  --cflags-begin \
    -DCK_MECHANISM=_CK_MECHANISM \
    -DCK_ATTRIBUTE=_CK_ATTRIBUTE \
    -DCK_VERSION=_CK_VERSION \
    -DCK_INFO=_CK_INFO \
    -DCK_MECHANISM_INFO=_CK_MECHANISM_INFO \
    -DCK_SESSION_INFO=_CK_SESSION_INFO \
    -DCK_SLOT_INFO=_CK_SLOT_INFO \
    -DCK_TOKEN_INFO=_CK_TOKEN_INFO \
    -DCK_FUNCTION_LIST=_CK_FUNCTION_LIST \
  --cflags-end \
  -I/usr/include \
  -I/usr/include/p11-kit-1/p11-kit \
  /usr/include/p11-kit-1/p11-kit/pkcs11.h \
  /usr/include/p11-kit-1/p11-kit/p11-kit.h

# 1. Remove introspectable="0" from CK_LONG alias and add name="glong"
sed -i -E 's/<alias name="CK_LONG" c:type="CK_LONG" introspectable="0">/<alias name="CK_LONG" c:type="CK_LONG">/' P11Kit-0.26.gir
sed -i -E '/<alias name="CK_LONG"/{n; n; s|<type c:type="long int"/>|<type name="glong" c:type="long int"/>|}' P11Kit-0.26.gir

# 2. Add c:type to callbacks (without introspectable)
sed -i -E 's/<callback name="([^"]+)">/<callback name="\1" c:type="\1">/g' P11Kit-0.26.gir

# 3. Add c:type to callbacks (with introspectable="0")
sed -i -E 's/<callback name="([^"]+)" introspectable="0">/<callback name="\1" c:type="\1" introspectable="0">/g' P11Kit-0.26.gir

# 4. Fix gpointer c:type ck_* -> CK_* uppercase
sed -i -E 's#(<type name="gpointer" c:type=")ck_([a-z0-9_]+)(\*?"/>)#\1CK_\U\2\E\3#g' P11Kit-0.26.gir

# 5. Replace CK_MECHANISM_TYPE inner type with gulong/unsigned long
sed -i -E 's|<type name="CK_MECHANISM_TYPE" c:type="CK_MECHANISM_TYPE"/>|<type name="gulong" c:type="unsigned long"/>|g' P11Kit-0.26.gir
