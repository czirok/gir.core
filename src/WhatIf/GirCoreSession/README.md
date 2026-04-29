# GirCoreSession

## Parity Checklist: gnome-session → GirCoreSession

### I. Leader (leader-systemd.c) → Program.cs — ✅ COMPLETE

| C Function                                 | C# Status |
| ------------------------------------------ | --------- |
| Connect to session bus                     | ✅        |
| `ResetFailed()` on systemd                 | ✅        |
| Env export (`SetEnvironment`)              | ✅        |
| Start `gnome-session@{name}.target`        | ✅        |
| Wait for `NameOwnerChanged` (signal-based) | ✅        |
| Monitor `SessionRunning` / `SessionOver`   | ✅        |
| SIGINT/SIGTERM → shutdown target           | ✅        |
| `gnome-session-shutdown.target`            | ✅        |

### II. Service (service-main.c + gsm-manager.c) — ✅ BASIC COMPLETE

This is the actual manager process: **it claims** the `org.gnome.SessionManager` name on the bus and serves the DBus API.

#### A. DBus Server Side — `SessionManagerHostService` + `SessionManagerServer` (service-main.c)

| Function                                       | Source                         |
| ---------------------------------------------- | ------------------------------ |
| Bus name ownership: `org.gnome.SessionManager` | ✅ `SessionManagerHostService` |
| `on_name_lost` → force exit                    | ✅ `SessionManagerHostService` |
| `gsm_manager_start()` after name acquired      | ✅ `SessionManagerHostService` |

#### B. Phase Machine — `GsmPhaseManager` (gsm-manager.c)

| Phase                                             | C Function                                                     |
| ------------------------------------------------- | -------------------------------------------------------------- |
| `INITIALIZATION` → wait for `Initialized()` calls | ✅                                                             |
| `APPLICATION` → start apps, wait for registration | ✅ autostart start + initial client registration stabilization |
| `RUNNING` → emit `SessionRunning`, stay           | ✅                                                             |
| `QUERY_END_SESSION` → broadcast query             | ✅ with hook points                                            |
| `END_SESSION` → broadcast end                     | ✅ with hook points                                            |
| `EXIT` → emit `SessionOver`, quit                 | ✅                                                             |

#### C. Client Management — `GsmClientStore` + `SessionManagerServer` (gsm-client.c)

| Method                                          | C Source |
| ----------------------------------------------- | -------- |
| `RegisterClient(app_id, startup_id)`            | ✅       |
| `UnregisterClient(client_id)`                   | ✅       |
| `ClientPrivate.EndSessionResponse(ok, reason)`  | ✅       |
| `ClientAdded` / `ClientRemoved` signal emission | ✅       |

#### D. Inhibitor Management — `GsmInhibitorStore` + `SessionManagerServer` (gsm-inhibitor.c)

| Method                                         | C Source |
| ---------------------------------------------- | -------- |
| `Inhibit(app_id, xid, reason, flags)` → cookie | ✅       |
| `Uninhibit(cookie)`                            | ✅       |
| `IsInhibited(flags)` → bool                    | ✅       |
| `GetInhibitors()` → object paths               | ✅       |
| `InhibitorAdded` / `InhibitorRemoved` signal   | ✅       |
| `InhibitedActions` property                    | ✅       |

#### E. Session Fill — `GsmSessionFillService` (gsm-session-fill.c)

| Function                                           | C Source      |
| -------------------------------------------------- | ------------- |
| Search for `{session}.session` keyfile in XDG dirs | ✅            |
| Kiosk mode detection                               | ✅            |
| Load `$XDG_CONFIG_HOME/autostart/`                 | ✅ (dir list) |
| Load `$XDG_DATA_DIRS/gnome/autostart/`             | ✅ (dir list) |
| Load `$XDG_CONFIG_DIRS/autostart/`                 | ✅ (dir list) |

#### F. Shutdown/Power DBus Methods

| Method                             | Via                            |
| ---------------------------------- | ------------------------------ |
| `Shutdown()`                       | ✅ systemd `PowerOffAsync`     |
| `Reboot()`                         | ✅ systemd `RebootAsync`       |
| `Suspend()`                        | ✅ systemd `SuspendAsync`      |
| `CanShutdown/CanReboot/CanSuspend` | ✅ phase guard + systemd calls |
| `Logout(mode)`                     | ✅ start phase machine         |

#### G. Presence — `GsmPresenceService` + `PresenceIdleMonitorService` + `SessionManagerServer` (gsm-presence.c)

| Function                                        |     |
| ----------------------------------------------- | --- |
| `org.gnome.SessionManager.Presence` DBus object | ✅  |
| Idle detection (logind `IdleHint`)              | ✅  |

### Step by Step Status

1. ✅ **II-A + II-B**: DBus host + phase skeleton
2. ✅ **II-E**: Session fill service
3. ✅ **II-C**: Client management + end-session response
4. ✅ **II-D**: Inhibitor management
5. ✅ **II-F**: Shutdown/Reboot/Suspend server side
6. ✅ **II-G**: Presence object + IdleHint monitor

### Important Note

The above points are complete at the API and main control path level. Full C parity may require further deepening (e.g., detailed autostart lifecycle, client/inhibitor edge cases, finer logout policy).

### Commands used for DBus API generation

```bash
dotnet dbus codegen --bus session --service org.gnome.SessionManager   --protocol-api   --namespace GirCoreSession.DBus   --output SessionManager.cs

dotnet dbus codegen --bus system --service org.freedesktop.systemd1 --protocol-api --namespace GirCoreSession.DBus --interface org.freedesktop.systemd1.Manager:SystemdManager   --output Systemd.cs
```
