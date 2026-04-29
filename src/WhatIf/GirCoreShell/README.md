# GirCoreShell

## Parity Checklist: GNOME Shell / Mutter → GirCoreShell

This is not a C# port of GNOME Shell, but an independent .NET/GirCore shell that runs **instead of** GNOME Shell. The primary goal is to function as a stable Mutter-based display server and shell compatibility provider under a GNOME session.

### I. Bootstrap and Mutter Runtime — ✅ COMPLETE

| Function                                                   | Status |
| ---------------------------------------------------------- | ------ |
| `MetaContext` creation                                     | ✅     |
| `MetaContext.Configure()`                                  | ✅     |
| `MetaContext.Setup()`                                      | ✅     |
| `MetaContext.Start()`                                      | ✅     |
| `MetaContext.NotifyReady()`                                | ✅     |
| `RunMainLoop()`                                            | ✅     |
| Full `MetaContext` lifecycle on a dedicated runtime thread | ✅     |
| `READY=1` systemd notify                                   | ✅     |
| Wayland display server startup                             | ✅     |
| Xwayland managed services display                          | ✅     |
| GBM renderer startup without llvmpipe forcing              | ✅     |

### II. Mutter Plugin / Stage — ✅ BASIC COMPLETE

| Function                                    | Status           |
| ------------------------------------------- | ---------------- |
| C# `Meta.Plugin` GType registration         | ✅               |
| Plugin instance created                     | ✅               |
| Stage retrieval                             | ✅               |
| Runtime display snapshot via `Meta.Display` | ✅               |
| Background color setup                      | ✅               |
| Fullscreen background actor creation        | ✅               |
| Stage display                               | ✅               |
| `ShellReady=True` state                     | ✅               |
| Mouse/window drag trail bug fixed           | ✅               |
| Mini launcher UI                            | ✅ first version |

### III. Window Manager Callbacks — ✅ MINIMAL

| Callback                              | Status                   |
| ------------------------------------- | ------------------------ |
| `Minimize` / `Unminimize`             | ✅ immediate complete    |
| `Map` / `Destroy`                     | ✅ immediate complete    |
| `SizeChange`                          | ✅ immediate complete    |
| `SwitchWorkspace`                     | ✅ immediate complete    |
| `KillWindowEffects`                   | ✅ pending state cleanup |
| `ConfirmDisplayChange`                | ✅ automatic accept      |
| `ShowTilePreview` / `HideTilePreview` | ⚠️ log-only              |
| `ShowWindowMenu`                      | ⚠️ log-only              |
| `CreateCloseDialog`                   | ⚠️ no UI                 |
| `CreateInhibitShortcutsDialog`        | ⚠️ no UI                 |
| `KeybindingFilter`                    | ⚠️ no policy             |

### IV. `org.gnome.Shell` DBus API — ✅ PARTIAL

| API                                       | Status             |
| ----------------------------------------- | ------------------ |
| Bus name ownership: `org.gnome.Shell`     | ✅                 |
| Introspection                             | ✅                 |
| `Mode` property                           | ✅                 |
| `ShellVersion` property                   | ✅                 |
| `ShellReady` extra property               | ✅                 |
| `OverviewActive` property                 | ✅ state tracking  |
| `Eval`                                    | ✅ forbidden reply |
| `FocusSearch`                             | ⚠️ no-op           |
| `ShowOSD`                                 | ⚠️ no-op           |
| `ShowMonitorLabels` / `HideMonitorLabels` | ⚠️ no-op           |
| `FocusApp`                                | ⚠️ no-op           |
| `ShowApplications`                        | ⚠️ no-op           |
| `ScreenTransition`                        | ⚠️ no-op           |
| `GrabAccelerator(s)`                      | ⚠️ DBus tracking   |
| `UngrabAccelerator(s)`                    | ⚠️ DBus tracking   |
| `AcceleratorActivated` signal             | ❌                 |
| Real Mutter keybinding integration        | ❌                 |

### IV/b. `org.gnome.Shell.Introspect` — ✅ P0 BASIC COMPLETE

| API                                              | Status                         |
| ------------------------------------------------ | ------------------------------ |
| Bus name ownership: `org.gnome.Shell.Introspect` | ✅                             |
| Introspection                                    | ✅                             |
| `GetRunningApplications`                         | ✅ based on window snapshot    |
| `GetWindows`                                     | ✅ based on window snapshot    |
| `RunningApplicationsChanged` signal              | ✅ emitted on window change    |
| `WindowsChanged` signal                          | ✅ emitted on window change    |
| `AnimationsEnabled` property                     | ✅ fixed `true`                |
| `ScreenSize` property                            | ✅ from DisplayConfig snapshot |
| `version` property                               | ✅ `3`                         |
| Full Shell app model                             | ❌                             |

### V. `org.gnome.Mutter.DisplayConfig` — ✅ P0 BASIC COMPLETE

| API                                                  | Status                                |
| ---------------------------------------------------- | ------------------------------------- |
| Bus name ownership: `org.gnome.Mutter.DisplayConfig` | ✅                                    |
| Introspection                                        | ✅                                    |
| `GetCurrentState`                                    | ✅ real `Meta.Display` snapshot based |
| Logical monitor model                                | ✅ with one active primary monitor    |
| Monitor mode model                                   | ✅ with current size/refresh          |
| `PowerSaveMode` property                             | ✅ tracked                            |
| `SetBacklight`                                       | ⚠️ no-op                              |
| `GetResources`                                       | ⚠️ legacy empty reply                 |
| `ApplyMonitorsConfig`                                | ❌                                    |
| `MonitorsChanged` signal                             | ❌                                    |
| Real gamma/CTM/backlight handling                    | ❌                                    |

### VI. `org.gnome.Mutter.ServiceChannel` — ✅ NATIVE BRIDGE COMPLETE

| API                                                   | Status                 |
| ----------------------------------------------------- | ---------------------- |
| Bus name ownership: `org.gnome.Mutter.ServiceChannel` | ✅                     |
| Introspection                                         | ✅                     |
| `OpenWaylandServiceConnection`                        | ✅ native bridge       |
| `OpenWaylandConnection`                               | ✅ native bridge       |
| Experimental raw Wayland socket bridge                | ✅ removed             |
| Real Mutter service-client FD                         | ✅ boot tested working |

**Note:** The bridge uses Mutter's `meta_wayland_client_new_create`, `meta_wayland_client_take_client_fd`, and `meta_wayland_client_set_caps` paths. Client creation is scheduled on the GLib main context. Based on test logs, service-client `1` and `3` branches work with PID resolution, main-context scheduling, and native FD replies. The `window-tag` option is currently logged but omitted because the installed `libmutter-18.so.0` does not export the `meta_wayland_client_set_window_tag` symbol.

### VII. `org.gnome.ScreenSaver` — ✅ MINIMAL COMPLETE

| API                                         | Status                    |
| ------------------------------------------- | ------------------------- |
| Bus name ownership: `org.gnome.ScreenSaver` | ✅                        |
| Introspection                               | ✅                        |
| `GetActive`                                 | ✅                        |
| `SetActive`                                 | ✅                        |
| `GetActiveTime`                             | ✅                        |
| `ActiveChanged` signal                      | ✅                        |
| `Lock`                                      | ⚠️ replies but no lock UI |
| Screen shield / lock screen UI              | ❌                        |
| Logind lock/unlock integration              | ❌                        |

### VIII. Brightness API — ⚠️ STUB

| API                                              | Status                 |
| ------------------------------------------------ | ---------------------- |
| Bus name ownership: `org.gnome.Shell.Brightness` | ✅                     |
| `HasBrightnessControl`                           | ✅ fixed `false`       |
| `SetDimming`                                     | ⚠️ internal state only |
| `SetAutoBrightnessTarget`                        | ⚠️ internal state only |
| `BrightnessChanged` signal                       | ❌                     |
| Real backlight model                             | ❌                     |

### IX. Session Environment and systemd Override — ✅ WORKING

| Function                                | Status |
| --------------------------------------- | ------ |
| `org.gnome.Shell@user.service` override | ✅     |
| `XDG_SESSION_TYPE=wayland`              | ✅     |
| `XDG_SESSION_CLASS=user`                | ✅     |
| `XDG_CURRENT_DESKTOP=GNOME`             | ✅     |
| llvmpipe / software GL forcing removed  | ✅     |
| LocalSearch indexer starts              | ✅     |

**Note:** Most of the environment export is handled by the related `GirCoreSession` project.

## Step by Step Status

1. ✅ **Bootstrap**: session targets start, shell service launches.
2. ✅ **Mutter runtime**: `MetaContext` runs stably on dedicated runtime thread.
3. ✅ **Stage**: purple GirCore background visible, trail/render bug fixed.
4. ✅ **DisplayConfig P0**: `GetCurrentState` works.
5. ✅ **Session env**: `XDG_SESSION_CLASS=user` and others pass through, LocalSearch starts.
6. ✅ **ScreenSaver P0**: minimal `org.gnome.ScreenSaver` service works.
7. ✅ **ServiceChannel**: native Mutter bridge boot tested working.
8. ⚠️ **Accelerator API**: DBus tracking exists, real keybinding missing.
9. ⚠️ **Shell UI**: mini launcher exists, overview, OSD, lock UI still missing.
10. ✅ **Portal window/app introspection**: `org.gnome.Shell.Introspect` app list works.

## Current Major Gaps

| Area                     | Gap                                                              |
| ------------------------ | ---------------------------------------------------------------- |
| Shell UI                 | overview, quick settings, full panel                             |
| App model                | full app model, running app tracking, search                     |
| Window/app introspection | full app model, accurate focus/app-state                         |
| ServiceChannel           | `window-tag` option missing because Mutter symbol isn't exported |
| Accelerators             | Mutter keybinding + activation DBus signal                       |
| Notifications            | `org.freedesktop.Notifications`, UI, history                     |
| Screenshot/screencast    | Shell Screenshot/Screencast, PipeWire, portal integration        |
| Lock UI                  | screen shield, logind lock/unlock                                |
| Shutdown UX              | end-session dialog, logout/restart/shutdown UI                   |

## Next Recommended Steps

| Priority | Task                                                                 |
| -------- | -------------------------------------------------------------------- |
| P0       | Real Mutter keybinding integration for Accelerator API               |
| P1       | Minimal launcher UI and desktop entry discovery refinement           |
| P1       | Real implementation of `ShowApplications`, `FocusSearch`, `FocusApp` |
| P1       | Simple OSD                                                           |
| P2       | Notification daemon                                                  |
| P2       | Minimal screenshot support                                           |
| P2       | Find alternative for ServiceChannel `window-tag`                     |
