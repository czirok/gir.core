using System;
using System.Threading;
using System.Threading.Tasks;
using GirCoreSession.DBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tmds.DBus.Protocol;

namespace GirCoreSession.Services;

/// <summary>
/// Hosts org.gnome.SessionManager on DBus and forwards domain events as signals.
/// </summary>
internal sealed class SessionManagerHostService : IHostedService
{
    // Safety toggle for object-path signals. Default is false (signals enabled).
    // Can be overridden with configuration key: suppressObjectPathSignals=true.
    private readonly bool _suppressObjectPathSignals;

    private readonly SessionBusConnection _bus;
    private readonly SessionManagerServer _server;
    private readonly GsmPhaseManager _phaseManager;
    private readonly GsmClientStore _clients;
    private readonly GsmInhibitorStore _inhibitors;
    private readonly GsmPresenceService _presence;
    private readonly GsmAutostartService _autostart;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<SessionManagerHostService> _logger;
    private IDisposable? _applicationAutostartHook;
    private IDisposable? _applicationClientWaitHook;
    private IDisposable? _queryEndSessionClientHook;
    private IDisposable? _endSessionClientHook;
    private bool _enabled;
    private bool _started;

    public SessionManagerHostService(
        SessionBusConnection bus,
        SessionManagerServer server,
        GsmPhaseManager phaseManager,
        GsmClientStore clients,
        GsmInhibitorStore inhibitors,
        GsmPresenceService presence,
        GsmAutostartService autostart,
        IConfiguration configuration,
        IHostApplicationLifetime lifetime,
        ILogger<SessionManagerHostService> logger)
    {
        _bus = bus;
        _server = server;
        _phaseManager = phaseManager;
        _clients = clients;
        _inhibitors = inhibitors;
        _presence = presence;
        _autostart = autostart;
        _lifetime = lifetime;
        _logger = logger;
        _suppressObjectPathSignals =
            bool.TryParse(configuration["suppressObjectPathSignals"], out var suppress)
                ? suppress
                : false;
    }

    public void Enable() => _enabled = true;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_enabled)
        {
            _logger.LogDebug("SessionManager DBus server hosting is disabled.");
            return;
        }

        var connection = _bus.Connection;
        _phaseManager.PhaseChanged += OnPhaseChanged;
        _phaseManager.SessionRunning += OnSessionRunning;
        _phaseManager.SessionOver += OnSessionOver;
        _clients.ClientAdded += OnClientAdded;
        _clients.ClientRemoved += OnClientRemoved;
        _inhibitors.InhibitorAdded += OnInhibitorAdded;
        _inhibitors.InhibitorRemoved += OnInhibitorRemoved;
        _presence.StatusChanged += OnPresenceStatusChanged;
        _presence.StatusTextChanged += OnPresenceStatusTextChanged;

        _applicationAutostartHook = _phaseManager.RegisterApplicationHook(_autostart.RunApplicationAutostartAsync);
        _applicationClientWaitHook = _phaseManager.RegisterApplicationHook(_clients.WaitForInitialRegistrationStabilityAsync);
        _queryEndSessionClientHook = _phaseManager.RegisterQueryEndSessionHook(_clients.RunQueryEndSessionAsync);
        _endSessionClientHook = _phaseManager.RegisterEndSessionHook(_clients.RunEndSessionAsync);

        // Register for client query/end-session notifications to broadcast signals.
        _clients.QueryEndSessionStarting += OnQueryEndSessionStarting;
        _clients.EndSessionStarting += OnEndSessionStarting;

        connection.AddMethodHandler(_server);

        var acquired = await connection.TryRequestNameAsync(
            SessionManagerServer.ServiceName,
            RequestNameOptions.None);

        if (!acquired)
        {
            _logger.LogCritical(
                "Failed to acquire DBus name '{ServiceName}'. " +
                "Another process (e.g. gnome-session-service) already owns the name. " +
                "Check: busctl --user status {ServiceName}",
                SessionManagerServer.ServiceName,
                SessionManagerServer.ServiceName);
            _lifetime.StopApplication();
            return;
        }

        _started = true;
        _logger.LogInformation(
            "Object-path signal suppression is {State} (config: suppressObjectPathSignals).",
            _suppressObjectPathSignals ? "ENABLED" : "DISABLED");
        _logger.LogInformation("Acquired DBus name: {ServiceName}", SessionManagerServer.ServiceName);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_started)
            return;

        _started = false;
        _phaseManager.PhaseChanged -= OnPhaseChanged;
        _phaseManager.SessionRunning -= OnSessionRunning;
        _phaseManager.SessionOver -= OnSessionOver;
        _clients.ClientAdded -= OnClientAdded;
        _clients.ClientRemoved -= OnClientRemoved;
        _clients.QueryEndSessionStarting -= OnQueryEndSessionStarting;
        _clients.EndSessionStarting -= OnEndSessionStarting;
        _inhibitors.InhibitorAdded -= OnInhibitorAdded;
        _inhibitors.InhibitorRemoved -= OnInhibitorRemoved;
        _presence.StatusChanged -= OnPresenceStatusChanged;
        _presence.StatusTextChanged -= OnPresenceStatusTextChanged;
        _applicationAutostartHook?.Dispose();
        _applicationClientWaitHook?.Dispose();
        _queryEndSessionClientHook?.Dispose();
        _endSessionClientHook?.Dispose();
        _applicationAutostartHook = null;
        _applicationClientWaitHook = null;
        _queryEndSessionClientHook = null;
        _endSessionClientHook = null;

        var released = await _bus.Connection.ReleaseNameAsync(SessionManagerServer.ServiceName);
        if (!released)
        {
            _logger.LogWarning("DBus name was not released because it was no longer owned: {ServiceName}", SessionManagerServer.ServiceName);
            return;
        }

        _logger.LogInformation("Released DBus name: {ServiceName}", SessionManagerServer.ServiceName);
    }

    private void OnPhaseChanged(GsmManagerPhase phase)
    {
        _logger.LogInformation("Session manager phase changed: {Phase}", phase);
    }

    private void OnSessionRunning() => EmitSignal("SessionRunning");

    private void OnSessionOver() => EmitSignal("SessionOver");

    private void OnClientAdded(string objectPath)
    {
        _logger.LogInformation(
            "ClientAdded event received for path={Path}. suppression={Suppressed}",
            objectPath,
            _suppressObjectPathSignals);

        if (_suppressObjectPathSignals)
        {
            _logger.LogWarning("Suppressed DBus signal ClientAdded for path={Path} due to active safety guard.", objectPath);
            return;
        }

        EmitObjectPathSignal("ClientAdded", objectPath);
    }

    private void OnClientRemoved(string objectPath)
    {
        _logger.LogInformation(
            "ClientRemoved event received for path={Path}. suppression={Suppressed}",
            objectPath,
            _suppressObjectPathSignals);

        if (_suppressObjectPathSignals)
        {
            _logger.LogWarning("Suppressed DBus signal ClientRemoved for path={Path} due to active safety guard.", objectPath);
            return;
        }

        EmitObjectPathSignal("ClientRemoved", objectPath);
    }

    private void OnInhibitorAdded(string objectPath)
    {
        _logger.LogInformation(
            "InhibitorAdded event received for path={Path}. suppression={Suppressed}",
            objectPath,
            _suppressObjectPathSignals);

        if (_suppressObjectPathSignals)
        {
            _logger.LogWarning("Suppressed DBus signal InhibitorAdded for path={Path} due to active safety guard.", objectPath);
            return;
        }

        EmitObjectPathSignal("InhibitorAdded", objectPath);
    }

    private void OnInhibitorRemoved(string objectPath)
    {
        _logger.LogInformation(
            "InhibitorRemoved event received for path={Path}. suppression={Suppressed}",
            objectPath,
            _suppressObjectPathSignals);

        if (_suppressObjectPathSignals)
        {
            _logger.LogWarning("Suppressed DBus signal InhibitorRemoved for path={Path} due to active safety guard.", objectPath);
            return;
        }

        EmitObjectPathSignal("InhibitorRemoved", objectPath);
    }

    private void OnPresenceStatusChanged(uint status) =>
        EmitSignal(SessionManagerServer.PresencePath, SessionManagerServer.PresenceInterfaceName, "StatusChanged", "u", static (w, value) => w.WriteUInt32(value), status);

    private void OnPresenceStatusTextChanged(string statusText) =>
        EmitSignal(SessionManagerServer.PresencePath, SessionManagerServer.PresenceInterfaceName, "StatusTextChanged", "s", static (w, value) => w.WriteString(value), statusText);

    private void OnQueryEndSessionStarting()
    {
        _logger.LogInformation("Query-end-session phase: broadcasting QueryEndSession signal to all clients.");
        NotifyClientQueryEndSession();
    }

    private void OnEndSessionStarting()
    {
        _logger.LogInformation("End-session phase: broadcasting EndSession signal to all clients.");
        NotifyClientEndSession();
    }

    /// <summary>
    /// Broadcasts QueryEndSession signal to all registered clients.
    /// </summary>
    public void NotifyClientQueryEndSession()
    {
        var clientPaths = _clients.GetAllClientPaths();
        _logger.LogInformation("Broadcasting QueryEndSession to {ClientCount} clients...", clientPaths.Count);
        foreach (var clientPath in clientPaths)
        {
            EmitClientSignal(clientPath, "QueryEndSession", 0u);
        }
    }

    /// <summary>
    /// Broadcasts EndSession signal to all registered clients.
    /// </summary>
    public void NotifyClientEndSession()
    {
        var clientPaths = _clients.GetAllClientPaths();
        _logger.LogInformation("Broadcasting EndSession to {ClientCount} clients...", clientPaths.Count);
        foreach (var clientPath in clientPaths)
        {
            EmitClientSignal(clientPath, "EndSession", 0u);
        }
    }

    private void EmitClientSignal(string clientPath, string signalName, uint flags)
    {
        using var writer = _bus.Connection.GetMessageWriter();
        writer.WriteSignalHeader(
            null,
            clientPath,
            "org.gnome.SessionManager.ClientPrivate",
            signalName,
            "u");
        writer.WriteUInt32(flags);
        var sent = _bus.Connection.TrySendMessage(writer.CreateMessage());
        if (sent)
        {
            _logger.LogInformation("Emitted client signal: {SignalName} to {ClientPath}", signalName, clientPath);
        }
        else
        {
            _logger.LogError("Failed to send client signal: {SignalName} to {ClientPath}", signalName, clientPath);
        }
    }

    private void EmitSignal(string signalName)
    {
        using var writer = _bus.Connection.GetMessageWriter();
        writer.WriteSignalHeader(
            null,
            SessionManagerServer.ObjectPath,
            SessionManagerServer.InterfaceName,
            signalName,
            null);
        var sent = _bus.Connection.TrySendMessage(writer.CreateMessage());
        if (sent)
        {
            _logger.LogInformation("Emitted DBus signal: {SignalName}", signalName);
        }
        else
        {
            _logger.LogError("Failed to send DBus signal: {SignalName}", signalName);
        }
    }

    private void EmitSignal<T>(string signalName, string signature, Action<MessageWriter, T> writeBody, T value)
        => EmitSignal(SessionManagerServer.ObjectPath, SessionManagerServer.InterfaceName, signalName, signature, writeBody, value);

    private void EmitObjectPathSignal(string signalName, string objectPath)
    {
        if (!IsValidObjectPath(objectPath))
        {
            _logger.LogError("Refusing to emit DBus signal {SignalName}: invalid object path '{Path}'", signalName, objectPath);
            return;
        }

        using var writer = _bus.Connection.GetMessageWriter();
        writer.WriteSignalHeader(
            null,
            SessionManagerServer.ObjectPath,
            SessionManagerServer.InterfaceName,
            signalName,
            "o");
        writer.WriteObjectPath(new ObjectPath(objectPath));

        var sent = _bus.Connection.TrySendMessage(writer.CreateMessage());
        if (sent)
        {
            _logger.LogInformation("Emitted DBus signal: {SignalName} {Value}", signalName, objectPath);
        }
        else
        {
            _logger.LogError("Failed to send DBus signal: {SignalName} {Value}", signalName, objectPath);
        }
    }

    private static bool IsValidObjectPath(string path)
    {
        if (string.IsNullOrEmpty(path) || path[0] != '/')
            return false;

        if (path.Length > 1 && path[^1] == '/')
            return false;

        for (var i = 1; i < path.Length; i++)
        {
            var c = path[i];
            if (c == '/')
                continue;

            if ((c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                (c >= '0' && c <= '9') ||
                c == '_')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private void EmitSignal<T>(string path, string @interface, string signalName, string signature, Action<MessageWriter, T> writeBody, T value)
    {
        using var writer = _bus.Connection.GetMessageWriter();
        writer.WriteSignalHeader(null, path, @interface, signalName, signature);
        writeBody(writer, value);
        var sent = _bus.Connection.TrySendMessage(writer.CreateMessage());
        if (sent)
        {
            _logger.LogInformation("Emitted DBus signal: {SignalName} {Value}", signalName, value);
        }
        else
        {
            _logger.LogError("Failed to send DBus signal: {SignalName} {Value}", signalName, value);
        }
    }
}
