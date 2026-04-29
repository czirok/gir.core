using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GirCoreShell.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using Tmds.DBus.Protocol;

namespace GirCoreShell.DBus;

internal sealed class GnomeMutterServiceChannelServer : IPathMethodHandler
{
    public const string ServiceName = "org.gnome.Mutter.ServiceChannel";
    public const string InterfaceName = "org.gnome.Mutter.ServiceChannel";
    public const string ObjectPath = "/org/gnome/Mutter/ServiceChannel";

    private const uint PortalBackend = 1;
    private const uint FileChooserPortalBackend = 2;
    private const uint GlobalShortcutsPortalBackend = 3;

    private static readonly ReadOnlyMemory<byte> IntrospectionXml = """
<node>
  <interface name="org.gnome.Mutter.ServiceChannel">
    <method name="OpenWaylandServiceConnection">
      <arg name="service_client_type" type="u" direction="in"/>
      <arg name="fd" type="h" direction="out"/>
    </method>
    <method name="OpenWaylandConnection">
      <arg name="options" type="a{sv}" direction="in"/>
      <arg name="fd" type="h" direction="out"/>
    </method>
  </interface>
</node>
"""u8.ToArray();

    private readonly ILogger<GnomeMutterServiceChannelServer> _logger;
    private readonly SessionBusConnection _bus;
    private readonly ShellRuntimeState _runtimeState;
    private readonly Dictionary<uint, Meta.WaylandClient> _serviceClients = [];
    private readonly List<Meta.WaylandClient> _genericClients = [];
    private readonly object _sync = new();

    public GnomeMutterServiceChannelServer(
        SessionBusConnection bus,
        ShellRuntimeState runtimeState,
        ILogger<GnomeMutterServiceChannelServer> logger)
    {
        _bus = bus;
        _runtimeState = runtimeState;
        _logger = logger;

        _logger.LogInformation("Mutter ServiceChannel started. native_meta_wayland_client_bridge=True");
    }

    public string Path => ObjectPath;

    public bool HandlesChildPaths => false;

    public async ValueTask HandleMethodAsync(MethodContext context)
    {
        var request = context.Request;

        if (context.IsDBusIntrospectRequest)
        {
            context.ReplyIntrospectXml([IntrospectionXml]);
            return;
        }

        if (request.PathAsString != ObjectPath || request.InterfaceAsString != InterfaceName)
        {
            context.ReplyUnknownMethodError();
            return;
        }

        var reader = request.GetBodyReader();
        switch (request.MemberAsString)
        {
            case "OpenWaylandServiceConnection":
                {
                    var serviceClientType = reader.ReadUInt32();
                    _logger.LogInformation(
                        "DBus call: OpenWaylandServiceConnection(service_client_type={ServiceClientType}, sender={Sender}).",
                        serviceClientType,
                        request.SenderAsString ?? "<unknown>");

                    if (!IsValidServiceClientType(serviceClientType))
                    {
                        _logger.LogWarning(
                            "OpenWaylandServiceConnection rejected invalid service_client_type={ServiceClientType}.",
                            serviceClientType);
                        context.ReplyError(
                            "org.freedesktop.DBus.Error.InvalidArgs",
                            $"Invalid service client type: {serviceClientType}");
                        break;
                    }

                    var pid = await GetCallerPidAsync(request.SenderAsString).ConfigureAwait(false);
                    await ReplyNativeWaylandConnectionAsync(
                        context,
                        pid,
                        $"service-client:{serviceClientType}",
                        client =>
                        {
                            client.SetCaps(Meta.WaylandClientCaps.X11Interop);
                            lock (_sync)
                            {
                                _serviceClients[serviceClientType] = client;
                            }
                        });
                    break;
                }
            case "OpenWaylandConnection":
                {
                    var options = reader.ReadDictionaryOfStringToVariantValue();
                    var windowTag = TryGetStringOption(options, "window-tag");
                    _logger.LogInformation(
                        "DBus call: OpenWaylandConnection(options_count={OptionsCount}, window_tag={WindowTag}, sender={Sender}).",
                        options.Count,
                        windowTag ?? "<none>",
                        request.SenderAsString ?? "<unknown>");

                    var pid = await GetCallerPidAsync(request.SenderAsString).ConfigureAwait(false);
                    await ReplyNativeWaylandConnectionAsync(
                        context,
                        pid,
                        "generic-client",
                        client =>
                        {
                            if (!string.IsNullOrWhiteSpace(windowTag))
                            {
                                _logger.LogWarning(
                                    "ServiceChannel window-tag option is present but skipped because meta_wayland_client_set_window_tag is not exported by libmutter-18. window_tag={WindowTag}",
                                    windowTag);
                            }

                            lock (_sync)
                            {
                                _genericClients.Add(client);
                            }
                        });
                    break;
                }
            default:
                _logger.LogInformation("DBus call: {Member} is not supported yet.", request.MemberAsString);
                context.ReplyError(
                    "org.freedesktop.DBus.Error.NotSupported",
                    $"{InterfaceName}.{request.MemberAsString} is not implemented yet.");
                break;
        }
    }

    private async Task ReplyNativeWaylandConnectionAsync(
        MethodContext context,
        int pid,
        string purpose,
        Action<Meta.WaylandClient> configureClient)
    {
        try
        {
            var (metaContext, metaThreadId) = _runtimeState.GetMetaContext();
            if (metaContext is null)
            {
                _logger.LogWarning(
                    "ServiceChannel native Wayland client request rejected because MetaContext is not available. purpose={Purpose} pid={Pid}",
                    purpose,
                    pid);

                context.ReplyError(
                    "org.freedesktop.DBus.Error.Failed",
                    "GirCoreShell MetaContext is not available yet.");
                return;
            }

            _logger.LogInformation(
                "Scheduling Mutter Wayland client creation on GLib main context. purpose={Purpose} pid={Pid} current_thread={CurrentThreadId} meta_thread={MetaThreadId}",
                purpose,
                pid,
                Environment.CurrentManagedThreadId,
                metaThreadId);

            var fd = await CreateWaylandClientOnMainContextAsync(
                metaContext,
                pid,
                purpose,
                configureClient).ConfigureAwait(false);

            using var fdHandle = new SafeFileHandle(new IntPtr(fd), ownsHandle: true);
            using var writer = context.CreateReplyWriter("h");
            writer.WriteHandle(fdHandle);
            context.Reply(writer.CreateMessage());

            _logger.LogInformation(
                "DBus reply: native ServiceChannel Wayland fd sent. purpose={Purpose} pid={Pid} fd={Fd}",
                purpose,
                pid,
                fd);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create native ServiceChannel Wayland client. purpose={Purpose} pid={Pid}",
                purpose,
                pid);
            context.ReplyError(
                "org.freedesktop.DBus.Error.Failed",
                $"Failed to create native Wayland service connection: {ex.Message}");
        }
    }

    private Task<int> CreateWaylandClientOnMainContextAsync(
        Meta.Context metaContext,
        int pid,
        string purpose,
        Action<Meta.WaylandClient> configureClient)
    {
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new GLib.Internal.SourceFuncAsyncHandler(() =>
        {
            try
            {
                _logger.LogInformation(
                    "Creating Mutter Wayland client on GLib main context. purpose={Purpose} pid={Pid} current_thread={CurrentThreadId}",
                    purpose,
                    pid,
                    Environment.CurrentManagedThreadId);

                var client = Meta.WaylandClient.NewCreate(metaContext, pid);
                configureClient(client);
                var fd = client.TakeClientFd();

                _logger.LogInformation(
                    "Mutter Wayland client created on GLib main context. purpose={Purpose} pid={Pid} fd={Fd}",
                    purpose,
                    pid,
                    fd);

                tcs.TrySetResult(fd);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return false;
        });

        GLib.Internal.MainContext.Invoke(
            GLib.Internal.MainContextUnownedHandle.NullHandle,
            callback.NativeCallback,
            IntPtr.Zero);

        return tcs.Task;
    }

    private static bool IsValidServiceClientType(uint serviceClientType) =>
        serviceClientType is PortalBackend or FileChooserPortalBackend or GlobalShortcutsPortalBackend;

    private async Task<int> GetCallerPidAsync(string? sender)
    {
        if (string.IsNullOrWhiteSpace(sender))
        {
            var fallbackPid = Environment.ProcessId;
            _logger.LogWarning(
                "ServiceChannel caller has no DBus sender. Falling back to current process pid={Pid}.",
                fallbackPid);
            return fallbackPid;
        }

        try
        {
            var pid = await _bus.Connection.CallMethodAsync(
                CreateGetConnectionUnixProcessIdMessage(sender),
                static (Message message, object? _) =>
                {
                    var reader = message.GetBodyReader();
                    return reader.ReadUInt32();
                },
                this).ConfigureAwait(false);

            _logger.LogInformation(
                "Resolved ServiceChannel caller pid. sender={Sender} pid={Pid}",
                sender,
                pid);
            return checked((int)pid);
        }
        catch (Exception ex)
        {
            var fallbackPid = Environment.ProcessId;
            _logger.LogWarning(
                ex,
                "Failed to resolve ServiceChannel caller pid. sender={Sender}. Falling back to current process pid={Pid}.",
                sender,
                fallbackPid);
            return fallbackPid;
        }
    }

    private MessageBuffer CreateGetConnectionUnixProcessIdMessage(string sender)
    {
        var writer = _bus.Connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: "org.freedesktop.DBus",
            path: "/org/freedesktop/DBus",
            @interface: "org.freedesktop.DBus",
            member: "GetConnectionUnixProcessID",
            signature: "s");
        writer.WriteString(sender);
        return writer.CreateMessage();
    }

    private static string? TryGetStringOption(Dictionary<string, VariantValue> options, string key)
    {
        if (!options.TryGetValue(key, out var value))
            return null;

        try
        {
            return value.GetString();
        }
        catch
        {
            return null;
        }
    }
}
