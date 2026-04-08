#!/bin/bash
export LD_LIBRARY_PATH=/usr/lib/gnome-shell
export XDG_SESSION_TYPE=wayland
export XDG_RUNTIME_DIR=/run/user/1000
export MUTTER_DEBUG_VERBOSE=1
export G_MESSAGES_DEBUG=all

exec ./GirCoreMutter --display-server
