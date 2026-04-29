using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace GirCoreLauncher.Services;

[UnsupportedOSPlatform("Windows")]
[UnsupportedOSPlatform("macOS")]
public sealed class GirCoreLauncherCatalog
{
    private readonly ILogger<GirCoreLauncherCatalog> _logger;
    private readonly object _sync = new();
    private ShellAppEntry[] _apps = [];
    private DateTimeOffset _lastScan = DateTimeOffset.MinValue;

    public GirCoreLauncherCatalog(ILogger<GirCoreLauncherCatalog> logger)
    {
        _logger = logger;
        GioUnix.DesktopAppInfo.SetDesktopEnv("GNOME");
        _logger.LogInformation("[AppCatalog] Desktop environment set to GNOME for GioUnix.DesktopAppInfo filtering.");
    }

    public ShellAppEntry[] GetApps()
    {
        lock (_sync)
        {
            if (_apps.Length == 0 || DateTimeOffset.UtcNow - _lastScan > TimeSpan.FromSeconds(30))
                RescanLocked();

            return [.. _apps];
        }
    }

    public bool Launch(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        ShellAppEntry? app;
        lock (_sync)
        {
            if (_apps.Length == 0)
                RescanLocked();

            app = _apps.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.Ordinal));
        }

        if (app is null)
        {
            _logger.LogWarning("[AppCatalog.Launch] App id={AppId} was not found in current catalog.", id);
            return false;
        }

        try
        {
            _logger.LogInformation(
                "[AppCatalog.Launch] Launch requested. id={AppId} name={Name} file={DesktopFile}",
                app.Id,
                app.Name,
                app.DesktopFile);

            var info = GioUnix.DesktopAppInfo.NewFromFilename(app.DesktopFile);
            if (info is null)
            {
                _logger.LogWarning("[AppCatalog.Launch] DesktopAppInfo.NewFromFilename returned null. file={DesktopFile}", app.DesktopFile);
                return false;
            }

            var launched = info.Launch(files: null, context: null);
            _logger.LogInformation("[AppCatalog.Launch] Launch result id={AppId} launched={Launched}.", app.Id, launched);
            return launched;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppCatalog.Launch] Launch failed. id={AppId} file={DesktopFile}", app.Id, app.DesktopFile);
            return false;
        }
    }

    private void RescanLocked()
    {
        var byId = new Dictionary<string, ShellAppEntry>(StringComparer.Ordinal);
        var directories = GetApplicationDirectories();

        _logger.LogInformation("[AppCatalog.Scan] Starting desktop file scan. directory_count={DirectoryCount}", directories.Length);

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                _logger.LogDebug("[AppCatalog.Scan] Directory skipped, does not exist: {Directory}", directory);
                continue;
            }

            string[] files;
            try
            {
                files = Directory.GetFiles(directory, "*.desktop", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AppCatalog.Scan] Failed to enumerate directory={Directory}", directory);
                continue;
            }

            _logger.LogInformation("[AppCatalog.Scan] Directory={Directory} desktop_files={Count}", directory, files.Length);

            foreach (var file in files)
                TryAddDesktopFile(byId, file);
        }

        _apps = byId.Values
            .OrderBy(static app => app.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(static app => app.Id, StringComparer.Ordinal)
            .ToArray();
        _lastScan = DateTimeOffset.UtcNow;

        _logger.LogInformation("[AppCatalog.Scan] Completed. visible_apps={Count}", _apps.Length);
    }

    private void TryAddDesktopFile(Dictionary<string, ShellAppEntry> byId, string file)
    {
        try
        {
            var info = GioUnix.DesktopAppInfo.NewFromFilename(file);
            if (info is null)
            {
                _logger.LogDebug("[AppCatalog.Scan] Desktop file skipped, DesktopAppInfo is null. file={DesktopFile}", file);
                return;
            }

            if (info.GetIsHidden() || info.GetNodisplay() || !info.ShouldShow())
            {
                _logger.LogDebug("[AppCatalog.Scan] Desktop file hidden/not shown. file={DesktopFile}", file);
                return;
            }

            var id = info.GetId();
            if (string.IsNullOrWhiteSpace(id))
                id = Path.GetFileName(file);

            var name = info.GetDisplayName();
            if (string.IsNullOrWhiteSpace(name))
                name = info.GetName();

            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogDebug("[AppCatalog.Scan] Desktop file skipped, missing display name. file={DesktopFile}", file);
                return;
            }

            if (!byId.ContainsKey(id))
            {
                byId.Add(id, new ShellAppEntry(id, name, info.GetDescription(), file));
                _logger.LogDebug("[AppCatalog.Scan] App added. id={AppId} name={Name} file={DesktopFile}", id, name, file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AppCatalog.Scan] Desktop file skipped after error. file={DesktopFile}", file);
        }
    }

    private static string[] GetApplicationDirectories()
    {
        var directories = new List<string>();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(home))
            directories.Add(Path.Combine(home, ".local/share/applications"));

        var xdgDataDirs = Environment.GetEnvironmentVariable("XDG_DATA_DIRS");
        var dataDirs = string.IsNullOrWhiteSpace(xdgDataDirs)
            ? ["/usr/local/share", "/usr/share"]
            : xdgDataDirs.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var dataDir in dataDirs)
            directories.Add(Path.Combine(dataDir, "applications"));

        return directories.Distinct(StringComparer.Ordinal).ToArray();
    }
}

public sealed record ShellAppEntry(
    string Id,
    string Name,
    string? Description,
    string DesktopFile);
