using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace GirCoreShell.Services;

/// <summary>
/// Shared runtime state used by DI services and DBus surface.
/// </summary>
internal sealed class ShellRuntimeState
{
    private readonly object _sync = new();
    private bool _shellReady;
    private Meta.Context? _metaContext;
    private int? _metaContextThreadId;
    private uint _displaySerial = 1;
    private DisplayMonitorState[] _displayMonitors = [DisplayMonitorState.Fallback];
    private readonly Dictionary<ulong, WindowIntrospectionState> _windows = [];

    public ShellRuntimeState(IConfiguration configuration)
    {
        Mode = configuration["shell:mode"] ?? "user";
        ShellVersion = configuration["shell:version"] ?? "GirCoreShell/0.1.0";
    }

    public string Mode { get; }

    public string ShellVersion { get; }

    public bool ShellReady
    {
        get
        {
            lock (_sync)
            {
                return _shellReady;
            }
        }
    }

    public event Action<bool>? ShellReadyChanged;

    public event Action? WindowsChanged;

    public (Meta.Context? Context, int? ThreadId) GetMetaContext()
    {
        lock (_sync)
        {
            return (_metaContext, _metaContextThreadId);
        }
    }

    public void SetMetaContext(Meta.Context? context, int? threadId)
    {
        lock (_sync)
        {
            _metaContext = context;
            _metaContextThreadId = threadId;
        }
    }

    public DisplayConfigurationState GetDisplayConfiguration()
    {
        lock (_sync)
        {
            return new DisplayConfigurationState(_displaySerial, [.. _displayMonitors]);
        }
    }

    public DisplayConfigurationState UpdateDisplayConfiguration(DisplayMonitorState[] monitors)
    {
        ArgumentNullException.ThrowIfNull(monitors);

        lock (_sync)
        {
            _displaySerial++;
            _displayMonitors = monitors.Length == 0 ? [DisplayMonitorState.Fallback] : [.. monitors];
            return new DisplayConfigurationState(_displaySerial, [.. _displayMonitors]);
        }
    }

    public void SetShellReady(bool ready)
    {
        bool changed;
        lock (_sync)
        {
            changed = _shellReady != ready;
            _shellReady = ready;
        }

        if (changed)
        {
            ShellReadyChanged?.Invoke(ready);
        }
    }

    public WindowIntrospectionState[] GetWindows()
    {
        lock (_sync)
        {
            return [.. _windows.Values];
        }
    }

    public void UpsertWindow(WindowIntrospectionState window)
    {
        ArgumentNullException.ThrowIfNull(window);

        lock (_sync)
        {
            _windows[window.Id] = window;
        }

        WindowsChanged?.Invoke();
    }

    public void RemoveWindow(ulong id)
    {
        var removed = false;
        lock (_sync)
        {
            removed = _windows.Remove(id);
        }

        if (removed)
            WindowsChanged?.Invoke();
    }
}

internal sealed record DisplayConfigurationState(
    uint Serial,
    DisplayMonitorState[] Monitors);

internal sealed record DisplayMonitorState(
    int Index,
    string Connector,
    string Vendor,
    string Product,
    string Serial,
    int X,
    int Y,
    int Width,
    int Height,
    double Scale,
    double RefreshRate)
{
    public static DisplayMonitorState Fallback { get; } = new(
        Index: 0,
        Connector: "UNKNOWN-0",
        Vendor: "unknown",
        Product: "unknown",
        Serial: "unknown",
        X: 0,
        Y: 0,
        Width: 1024,
        Height: 768,
        Scale: 1.0,
        RefreshRate: 60.0);
}

internal sealed record WindowIntrospectionState(
    ulong Id,
    string AppId,
    string? Title,
    string? WmClass,
    uint ClientType,
    bool IsHidden,
    bool HasFocus,
    uint Width,
    uint Height,
    string? SandboxedAppId);
