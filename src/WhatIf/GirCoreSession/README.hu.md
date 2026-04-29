# GirCoreSession

## Paritás-checklist: gnome-session → GirCoreSession

### I. Leader (leader-systemd.c) → Program.cs — ✅ KÉSZ

| C függvény                                | C# állapot |
| ----------------------------------------- | ---------- |
| Kapcsolódás session bushoz                | ✅         |
| `ResetFailed()` systemd-en                | ✅         |
| Env export (`SetEnvironment`)             | ✅         |
| `gnome-session@{name}.target` indítása    | ✅         |
| `NameOwnerChanged` várás (signal-based)   | ✅         |
| `SessionRunning` / `SessionOver` figyelés | ✅         |
| SIGINT/SIGTERM → shutdown target          | ✅         |
| `gnome-session-shutdown.target`           | ✅         |

### II. Service (service-main.c + gsm-manager.c) — ✅ ALAP KÉSZ

Ez az igazi manager folyamat: **saját maga adja ki** az `org.gnome.SessionManager` nevet a buszra, és kiszolgálja a DBus API-t.

#### A. DBus szerver oldal — `SessionManagerHostService` + `SessionManagerServer` (service-main.c)

| Funkció                                        | Forrás                         |
| ---------------------------------------------- | ------------------------------ |
| Bus name ownership: `org.gnome.SessionManager` | ✅ `SessionManagerHostService` |
| `on_name_lost` → force exit                    | ✅ `SessionManagerHostService` |
| `gsm_manager_start()` after name acquired      | ✅ `SessionManagerHostService` |

#### B. Fázis gép — `GsmPhaseManager` (gsm-manager.c)

| Fázis                                             | C függvény                                                      |
| ------------------------------------------------- | --------------------------------------------------------------- |
| `INITIALIZATION` → wait for `Initialized()` calls | ✅                                                              |
| `APPLICATION` → start apps, wait for registration | ✅ autostart indítás + kezdeti kliens-regisztráció stabilizáció |
| `RUNNING` → emit `SessionRunning`, stay           | ✅                                                              |
| `QUERY_END_SESSION` → broadcast query             | ✅ hook-pontokkal                                               |
| `END_SESSION` → broadcast end                     | ✅ hook-pontokkal                                               |
| `EXIT` → emit `SessionOver`, quit                 | ✅                                                              |

#### C. Kliens kezelés — `GsmClientStore` + `SessionManagerServer` (gsm-client.c)

| Metódus                                         | C forrás |
| ----------------------------------------------- | -------- |
| `RegisterClient(app_id, startup_id)`            | ✅       |
| `UnregisterClient(client_id)`                   | ✅       |
| `ClientPrivate.EndSessionResponse(ok, reason)`  | ✅       |
| `ClientAdded` / `ClientRemoved` signal emisszió | ✅       |

#### D. Inhibitor kezelés — `GsmInhibitorStore` + `SessionManagerServer` (gsm-inhibitor.c)

| Metódus                                        | C forrás |
| ---------------------------------------------- | -------- |
| `Inhibit(app_id, xid, reason, flags)` → cookie | ✅       |
| `Uninhibit(cookie)`                            | ✅       |
| `IsInhibited(flags)` → bool                    | ✅       |
| `GetInhibitors()` → object paths               | ✅       |
| `InhibitorAdded` / `InhibitorRemoved` signal   | ✅       |
| `InhibitedActions` property                    | ✅       |

#### E. Session fill — `GsmSessionFillService` (gsm-session-fill.c)

| Funkció                                          | C forrás       |
| ------------------------------------------------ | -------------- |
| `{session}.session` keyfile keresés XDG dirs-ben | ✅             |
| Kiosk mode detektálás                            | ✅             |
| `$XDG_CONFIG_HOME/autostart/` betöltés           | ✅ (dir lista) |
| `$XDG_DATA_DIRS/gnome/autostart/` betöltés       | ✅ (dir lista) |
| `$XDG_CONFIG_DIRS/autostart/` betöltés           | ✅ (dir lista) |

#### F. Shutdown/Power DBus metódusok

| Metódus                            | Via                              |
| ---------------------------------- | -------------------------------- |
| `Shutdown()`                       | ✅ systemd `PowerOffAsync`       |
| `Reboot()`                         | ✅ systemd `RebootAsync`         |
| `Suspend()`                        | ✅ systemd `SuspendAsync`        |
| `CanShutdown/CanReboot/CanSuspend` | ✅ phase guard + systemd hívások |
| `Logout(mode)`                     | ✅ fázis gép indítása            |

#### G. Presence — `GsmPresenceService` + `PresenceIdleMonitorService` + `SessionManagerServer` (gsm-presence.c)

| Funkció                                         |     |
| ----------------------------------------------- | --- |
| `org.gnome.SessionManager.Presence` DBus object | ✅  |
| Idle detektálás (logind `IdleHint`)             | ✅  |

### Lépésről Lépésre Státusz

1. ✅ **II-A + II-B**: DBus host + fázis váz
2. ✅ **II-E**: Session fill szolgáltatás
3. ✅ **II-C**: Kliens kezelés + end-session response
4. ✅ **II-D**: Inhibitor kezelés
5. ✅ **II-F**: Shutdown/Reboot/Suspend kiszolgáló oldal
6. ✅ **II-G**: Presence objektum + IdleHint monitor

### Fontos Megjegyzés

A fenti pontok az API és fő vezérlési útvonalak szintjén készek. A teljes C parity további mélyítést igényelhet (pl. részletes autostart lifecycle, client/inhibitor edge case-ek, finomabb logout-policy).

### DBus API generálásához használt parancsok

```bash
dotnet dbus codegen --bus session --service org.gnome.SessionManager   --protocol-api   --namespace GirCoreSession.DBus   --output SessionManager.cs

dotnet dbus codegen --bus system --service org.freedesktop.systemd1 --protocol-api --namespace GirCoreSession.DBus --interface org.freedesktop.systemd1.Manager:SystemdManager   --output Systemd.cs
```
