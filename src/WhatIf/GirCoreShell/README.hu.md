# GirCoreShell

## Paritás-checklist: GNOME Shell / Mutter → GirCoreShell

Ez nem GNOME Shell C# port, hanem egy önálló, GNOME Shell helyett futó
.NET/GirCore shell. A cél első körben az, hogy a GNOME session alatt stabil
Mutter-alapú display serverként és shell-kompatibilitási szolgáltatóként
működjön.

### I. Bootstrap és Mutter runtime — ✅ KÉSZ

| Funkció                                                     | Állapot |
| ----------------------------------------------------------- | ------- |
| `MetaContext` létrehozás                                    | ✅      |
| `MetaContext.Configure()`                                   | ✅      |
| `MetaContext.Setup()`                                       | ✅      |
| `MetaContext.Start()`                                       | ✅      |
| `MetaContext.NotifyReady()`                                 | ✅      |
| `RunMainLoop()`                                             | ✅      |
| Teljes `MetaContext` életciklus egy dedikált runtime szálon | ✅      |
| `READY=1` systemd notify                                    | ✅      |
| Wayland display server indulás                              | ✅      |
| Xwayland managed services display                           | ✅      |
| GBM renderer indulás llvmpipe kényszerítés nélkül           | ✅      |

### II. Mutter plugin / stage — ✅ ALAP KÉSZ

| Funkció                                                  | Állapot          |
| -------------------------------------------------------- | ---------------- |
| C# `Meta.Plugin` GType regisztráció                      | ✅               |
| Plugin példány létrejön                                  | ✅               |
| Stage lekérése                                           | ✅               |
| Runtime display snapshot lekérése `Meta.Display` alapján | ✅               |
| Háttérszín beállítása                                    | ✅               |
| Fullscreen háttér actor létrehozása                      | ✅               |
| Stage megjelenítése                                      | ✅               |
| `ShellReady=True` állapot                                | ✅               |
| Egér/ablakmozgatási trail hiba megszűnt                  | ✅               |
| Mini launcher UI                                         | ✅ első változat |

### III. Window manager callbackek — ✅ MINIMÁLIS

| Callback                              | Állapot                      |
| ------------------------------------- | ---------------------------- |
| `Minimize` / `Unminimize`             | ✅ azonnali complete         |
| `Map` / `Destroy`                     | ✅ azonnali complete         |
| `SizeChange`                          | ✅ azonnali complete         |
| `SwitchWorkspace`                     | ✅ azonnali complete         |
| `KillWindowEffects`                   | ✅ pending állapot takarítás |
| `ConfirmDisplayChange`                | ✅ automatikus elfogadás     |
| `ShowTilePreview` / `HideTilePreview` | ⚠️ log-only                  |
| `ShowWindowMenu`                      | ⚠️ log-only                  |
| `CreateCloseDialog`                   | ⚠️ nincs UI                  |
| `CreateInhibitShortcutsDialog`        | ⚠️ nincs UI                  |
| `KeybindingFilter`                    | ⚠️ nincs policy              |

### IV. `org.gnome.Shell` DBus API — ✅ RÉSZLEGES

| API                                       | Állapot             |
| ----------------------------------------- | ------------------- |
| Bus name ownership: `org.gnome.Shell`     | ✅                  |
| Introspection                             | ✅                  |
| `Mode` property                           | ✅                  |
| `ShellVersion` property                   | ✅                  |
| `ShellReady` extra property               | ✅                  |
| `OverviewActive` property                 | ✅ állapotkönyvelés |
| `Eval`                                    | ✅ tiltott válasz   |
| `FocusSearch`                             | ⚠️ no-op            |
| `ShowOSD`                                 | ⚠️ no-op            |
| `ShowMonitorLabels` / `HideMonitorLabels` | ⚠️ no-op            |
| `FocusApp`                                | ⚠️ no-op            |
| `ShowApplications`                        | ⚠️ no-op            |
| `ScreenTransition`                        | ⚠️ no-op            |
| `GrabAccelerator(s)`                      | ⚠️ DBus könyvelés   |
| `UngrabAccelerator(s)`                    | ⚠️ DBus könyvelés   |
| `AcceleratorActivated` signal             | ❌                  |
| Valódi Mutter keybinding bekötés          | ❌                  |

### IV/b. `org.gnome.Shell.Introspect` — ✅ P0 ALAP KÉSZ

| API                                              | Állapot                        |
| ------------------------------------------------ | ------------------------------ |
| Bus name ownership: `org.gnome.Shell.Introspect` | ✅                             |
| Introspection                                    | ✅                             |
| `GetRunningApplications`                         | ✅ window snapshot alapján     |
| `GetWindows`                                     | ✅ window snapshot alapján     |
| `RunningApplicationsChanged` signal              | ✅ emitálva window változáskor |
| `WindowsChanged` signal                          | ✅ emitálva window változáskor |
| `AnimationsEnabled` property                     | ✅ fix `true`                  |
| `ScreenSize` property                            | ✅ DisplayConfig snapshotból   |
| `version` property                               | ✅ `3`                         |
| Teljes Shell app modell                          | ❌                             |

### V. `org.gnome.Mutter.DisplayConfig` — ✅ P0 ALAP KÉSZ

| API                                                  | Állapot                                  |
| ---------------------------------------------------- | ---------------------------------------- |
| Bus name ownership: `org.gnome.Mutter.DisplayConfig` | ✅                                       |
| Introspection                                        | ✅                                       |
| `GetCurrentState`                                    | ✅ valós `Meta.Display` snapshot alapján |
| Logical monitor modell                               | ✅ egy aktív primary monitorral          |
| Monitor mode modell                                  | ✅ aktuális mérettel/frissítéssel        |
| `PowerSaveMode` property                             | ✅ könyvelt                              |
| `SetBacklight`                                       | ⚠️ no-op                                 |
| `GetResources`                                       | ⚠️ legacy üres válasz                    |
| `ApplyMonitorsConfig`                                | ❌                                       |
| `MonitorsChanged` signal                             | ❌                                       |
| Gamma/CTM/backlight valódi kezelés                   | ❌                                       |

### VI. `org.gnome.Mutter.ServiceChannel` — ✅ NATÍV BRIDGE KÉSZ

| API                                                   | Állapot                 |
| ----------------------------------------------------- | ----------------------- |
| Bus name ownership: `org.gnome.Mutter.ServiceChannel` | ✅                      |
| Introspection                                         | ✅                      |
| `OpenWaylandServiceConnection`                        | ✅ natív bridge         |
| `OpenWaylandConnection`                               | ✅ natív bridge         |
| Kísérleti raw Wayland socket bridge                   | ✅ eltávolítva          |
| Valódi Mutter service-client FD                       | ✅ boot teszten működik |

Megjegyzés: a bridge a Mutter `meta_wayland_client_new_create`,
`meta_wayland_client_take_client_fd` és `meta_wayland_client_set_caps` útját
használja. A kliens létrehozása a GLib main contextre van ütemezve. A
tesztlog alapján a service-client `1` és `3` ág PID-feloldással, main-context
ütemezéssel és natív FD válasszal működik. A `window-tag` opció egyelőre
logolva van, de kihagyjuk, mert a telepített `libmutter-18.so.0` nem exportálja
a `meta_wayland_client_set_window_tag` szimbólumot.

### VII. `org.gnome.ScreenSaver` — ✅ MINIMÁLIS KÉSZ

| API                                         | Állapot                       |
| ------------------------------------------- | ----------------------------- |
| Bus name ownership: `org.gnome.ScreenSaver` | ✅                            |
| Introspection                               | ✅                            |
| `GetActive`                                 | ✅                            |
| `SetActive`                                 | ✅                            |
| `GetActiveTime`                             | ✅                            |
| `ActiveChanged` signal                      | ✅                            |
| `Lock`                                      | ⚠️ válaszol, de nincs lock UI |
| Screen shield / lock screen UI              | ❌                            |
| Logind lock/unlock integráció               | ❌                            |

### VIII. Brightness API — ⚠️ STUB

| API                                              | Állapot               |
| ------------------------------------------------ | --------------------- |
| Bus name ownership: `org.gnome.Shell.Brightness` | ✅                    |
| `HasBrightnessControl`                           | ✅ fix `false`        |
| `SetDimming`                                     | ⚠️ csak belső állapot |
| `SetAutoBrightnessTarget`                        | ⚠️ csak belső állapot |
| `BrightnessChanged` signal                       | ❌                    |
| Valódi backlight modell                          | ❌                    |

### IX. Session környezet és systemd override — ✅ RENDBEN

| Funkció                                         | Állapot |
| ----------------------------------------------- | ------- |
| `org.gnome.Shell@user.service` override         | ✅      |
| `XDG_SESSION_TYPE=wayland`                      | ✅      |
| `XDG_SESSION_CLASS=user`                        | ✅      |
| `XDG_CURRENT_DESKTOP=GNOME`                     | ✅      |
| llvmpipe / software GL kényszerítés eltávolítva | ✅      |
| LocalSearch indexer indul                       | ✅      |

Megjegyzés: a környezet export fő része a kapcsolódó `GirCoreSession`
projektben van.

## Lépésről Lépésre Státusz

1. ✅ **Bootstrap**: session célok indulnak, shell service elindul.
2. ✅ **Mutter runtime**: `MetaContext` stabilan fut, dedikált runtime szálon.
3. ✅ **Stage**: lila GirCore háttér látszik, trail/render hiba megszűnt.
4. ✅ **DisplayConfig P0**: `GetCurrentState` működik.
5. ✅ **Session env**: `XDG_SESSION_CLASS=user` és társai átmennek, LocalSearch indul.
6. ✅ **ScreenSaver P0**: minimális `org.gnome.ScreenSaver` service működik.
7. ✅ **ServiceChannel**: natív Mutter bridge boot teszten működik.
8. ⚠️ **Accelerator API**: DBus könyvelés van, valódi keybinding nincs.
9. ⚠️ **Shell UI**: mini launcher van, overview, OSD, lock UI még nincs.
10. ✅ **Portal window/app introspection**: `org.gnome.Shell.Introspect` app listája működik.

## Jelenlegi Fő Hiányok

| Terület                  | Hiány                                                                 |
| ------------------------ | --------------------------------------------------------------------- |
| Shell UI                 | overview, quick settings, teljes panel                                |
| App modell               | teljes app modell, futó app követés, keresés                          |
| Window/app introspection | teljes app modell, pontos fókusz/app-state                            |
| ServiceChannel           | `window-tag` opció hiányzik, mert a Mutter szimbólum nincs exportálva |
| Accelerators             | Mutter keybinding + aktivációs DBus signal                            |
| Notifications            | `org.freedesktop.Notifications`, UI, history                          |
| Screenshot/screencast    | Shell Screenshot/Screencast, PipeWire, portal integráció              |
| Lock UI                  | screen shield, logind lock/unlock                                     |
| Shutdown UX              | end-session dialog, logout/restart/shutdown UI                        |

## Következő Javasolt Lépések

| Prioritás | Feladat                                                      |
| --------- | ------------------------------------------------------------ |
| P0        | Accelerator API valódi Mutter keybinding bekötése            |
| P1        | Minimális launcher UI és desktop entry discovery finomítása  |
| P1        | `ShowApplications`, `FocusSearch`, `FocusApp` valós bekötése |
| P1        | Egyszerű OSD                                                 |
| P2        | Notification daemon                                          |
| P2        | Screenshot minimum                                           |
| P2        | ServiceChannel `window-tag` alternatíva keresése             |
