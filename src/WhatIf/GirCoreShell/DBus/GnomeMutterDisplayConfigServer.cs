using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GirCoreShell.Services;
using Microsoft.Extensions.Logging;
using Tmds.DBus.Protocol;

namespace GirCoreShell.DBus;

/// <summary>
/// Minimal DBus stub for org.gnome.Mutter.DisplayConfig.
/// It currently only guarantees name ownership and PowerSaveMode property handling.
/// </summary>
internal sealed class GnomeMutterDisplayConfigServer : IPathMethodHandler
{
    public const string ServiceName = "org.gnome.Mutter.DisplayConfig";
    public const string InterfaceName = "org.gnome.Mutter.DisplayConfig";
    public const string ObjectPath = "/org/gnome/Mutter/DisplayConfig";

    private static readonly ReadOnlyMemory<byte> IntrospectionXml = """
<node>
  <interface name="org.gnome.Mutter.DisplayConfig">
    <method name="GetResources">
      <arg name="serial" direction="out" type="u"/>
      <arg name="crtcs" direction="out" type="a(uxiiiiiuaua{sv})"/>
      <arg name="outputs" direction="out" type="a(uxiausauaua{sv})"/>
      <arg name="modes" direction="out" type="a(uxuudu)"/>
      <arg name="max_screen_width" direction="out" type="i"/>
      <arg name="max_screen_height" direction="out" type="i"/>
    </method>
    <method name="GetCurrentState">
      <arg name="serial" direction="out" type="u"/>
      <arg name="monitors" direction="out" type="a((ssss)a(siiddada{sv})a{sv})"/>
      <arg name="logical_monitors" direction="out" type="a(iiduba(ssss)a{sv})"/>
      <arg name="properties" direction="out" type="a{sv}"/>
    </method>
    <method name="SetBacklight">
      <arg name="serial" direction="in" type="u"/>
      <arg name="connector" direction="in" type="s"/>
      <arg name="value" direction="in" type="i"/>
    </method>
    <property name="PowerSaveMode" type="i" access="readwrite"/>
    <property name="PanelOrientationManaged" type="b" access="read"/>
    <property name="ApplyMonitorsConfigAllowed" type="b" access="read"/>
    <property name="NightLightSupported" type="b" access="read"/>
    <property name="HasExternalMonitor" type="b" access="read"/>
  </interface>
</node>
"""u8.ToArray();

    private const uint TransformNormal = 0;
    private const uint LayoutModeLogical = 1;

    private readonly ShellRuntimeState _runtimeState;
    private readonly ILogger<GnomeMutterDisplayConfigServer> _logger;
    private int _powerSaveMode;

    public GnomeMutterDisplayConfigServer(
        ShellRuntimeState runtimeState,
        ILogger<GnomeMutterDisplayConfigServer> logger)
    {
        _runtimeState = runtimeState;
        _logger = logger;
    }

    public string Path => ObjectPath;

    public bool HandlesChildPaths => false;

    public ValueTask HandleMethodAsync(MethodContext context)
    {
        var request = context.Request;

        if (context.IsDBusIntrospectRequest)
        {
            context.ReplyIntrospectXml([IntrospectionXml]);
            return ValueTask.CompletedTask;
        }

        if (request.PathAsString != ObjectPath)
        {
            context.ReplyUnknownMethodError();
            return ValueTask.CompletedTask;
        }

        if (context.IsPropertiesInterfaceRequest)
        {
            HandleProperties(context);
            return ValueTask.CompletedTask;
        }

        if (request.InterfaceAsString != InterfaceName)
        {
            context.ReplyUnknownMethodError();
            return ValueTask.CompletedTask;
        }

        var reader = request.GetBodyReader();

        switch (request.MemberAsString)
        {
            case "GetResources":
                _logger.LogInformation("DBus call: GetResources. Returning legacy empty resource snapshot.");
                ReplyGetResources(context);
                break;
            case "GetCurrentState":
                {
                    var snapshot = _runtimeState.GetDisplayConfiguration();
                    _logger.LogInformation(
                        "DBus call: GetCurrentState. serial={Serial} monitors={MonitorCount}",
                        snapshot.Serial,
                        snapshot.Monitors.Length);

                    ReplyGetCurrentState(context, snapshot);
                    break;
                }
            case "SetBacklight":
                var backlightSerial = reader.ReadUInt32();
                var backlightConnector = reader.ReadString();
                var backlightValue = reader.ReadInt32();
                _logger.LogInformation(
                    "DBus call: SetBacklight(serial={Serial}, connector={Connector}, value={Value}) no-op.",
                    backlightSerial,
                    backlightConnector,
                    backlightValue);
                ReplyEmpty(context);
                break;
            default:
                context.ReplyError(
                    "org.freedesktop.DBus.Error.NotSupported",
                    $"org.gnome.Mutter.DisplayConfig.{request.MemberAsString} is not implemented yet.");
                break;
        }

        return ValueTask.CompletedTask;
    }

    private void HandleProperties(MethodContext context)
    {
        var request = context.Request;
        var reader = request.GetBodyReader();

        switch (request.MemberAsString)
        {
            case "Get":
                {
                    var interfaceName = reader.ReadString();
                    var propertyName = reader.ReadString();
                    if (interfaceName != InterfaceName)
                    {
                        context.ReplyError("org.freedesktop.DBus.Error.InvalidArgs", $"Unsupported interface '{interfaceName}'.");
                        return;
                    }

                    ReplyProperty(context, propertyName);
                    return;
                }
            case "GetAll":
                {
                    var interfaceName = reader.ReadString();
                    if (interfaceName != InterfaceName)
                    {
                        context.ReplyError("org.freedesktop.DBus.Error.InvalidArgs", $"Unsupported interface '{interfaceName}'.");
                        return;
                    }

                    using var writer = context.CreateReplyWriter("a{sv}");
                    writer.WriteDictionary(GetAllProperties());
                    context.Reply(writer.CreateMessage());
                    return;
                }
            case "Set":
                {
                    var interfaceName = reader.ReadString();
                    var propertyName = reader.ReadString();
                    if (interfaceName != InterfaceName)
                    {
                        context.ReplyError("org.freedesktop.DBus.Error.InvalidArgs", $"Unsupported interface '{interfaceName}'.");
                        return;
                    }

                    if (propertyName != "PowerSaveMode")
                    {
                        context.ReplyError("org.freedesktop.DBus.Error.InvalidArgs", $"Property '{propertyName}' is not writable.");
                        return;
                    }

                    _powerSaveMode = reader.ReadVariantValue().GetInt32();
                    _logger.LogInformation("DBus property set: PowerSaveMode={PowerSaveMode}", _powerSaveMode);
                    ReplyEmpty(context);
                    return;
                }
            default:
                context.ReplyUnknownMethodError();
                return;
        }
    }

    private void ReplyProperty(MethodContext context, string propertyName)
    {
        switch (propertyName)
        {
            case "PowerSaveMode":
                using (var writer = context.CreateReplyWriter("v"))
                {
                    writer.WriteVariantInt32(_powerSaveMode);
                    context.Reply(writer.CreateMessage());
                }
                break;
            case "PanelOrientationManaged":
            case "ApplyMonitorsConfigAllowed":
            case "NightLightSupported":
            case "HasExternalMonitor":
                using (var writer = context.CreateReplyWriter("v"))
                {
                    writer.WriteVariantBool(false);
                    context.Reply(writer.CreateMessage());
                }
                break;
            default:
                context.ReplyError("org.freedesktop.DBus.Error.InvalidArgs", $"Unknown property '{propertyName}'.");
                break;
        }
    }

    private KeyValuePair<string, VariantValue>[] GetAllProperties() =>
    [
        new("PowerSaveMode", VariantValue.Int32(_powerSaveMode)),
        new("PanelOrientationManaged", VariantValue.Bool(false)),
        new("ApplyMonitorsConfigAllowed", VariantValue.Bool(false)),
        new("NightLightSupported", VariantValue.Bool(false)),
        new("HasExternalMonitor", VariantValue.Bool(false))
    ];

    private static void ReplyGetResources(MethodContext context)
    {
        using var writer = context.CreateReplyWriter("ua(uxiiiiiuaua{sv})a(uxiausauaua{sv})a(uxuudu)ii");

        writer.WriteUInt32(1);

        var crtcsStart = writer.WriteArrayStart(DBusType.Struct);
        writer.WriteArrayEnd(crtcsStart);

        var outputsStart = writer.WriteArrayStart(DBusType.Struct);
        writer.WriteArrayEnd(outputsStart);

        var modesStart = writer.WriteArrayStart(DBusType.Struct);
        writer.WriteArrayEnd(modesStart);

        writer.WriteInt32(0);
        writer.WriteInt32(0);

        context.Reply(writer.CreateMessage());
    }

    private void ReplyGetCurrentState(MethodContext context, DisplayConfigurationState snapshot)
    {
        var writer = context.CreateReplyWriter("ua((ssss)a(siiddada{sv})a{sv})a(iiduba(ssss)a{sv})a{sv}");
        try
        {
            writer.WriteUInt32(snapshot.Serial);
            WriteMonitors(ref writer, snapshot.Monitors);
            WriteLogicalMonitors(ref writer, snapshot.Monitors);
            writer.WriteDictionary(new KeyValuePair<string, VariantValue>[]
            {
                new("layout-mode", VariantValue.UInt32(LayoutModeLogical)),
                new("supports-changing-layout-mode", VariantValue.Bool(false)),
                new("global-scale-required", VariantValue.Bool(false))
            });

            context.Reply(writer.CreateMessage());
            _logger.LogInformation(
                "DBus reply: GetCurrentState completed. serial={Serial} monitors={MonitorCount} logical_monitors={LogicalMonitorCount}",
                snapshot.Serial,
                snapshot.Monitors.Length,
                snapshot.Monitors.Length);
        }
        finally
        {
            writer.Dispose();
        }
    }

    private void WriteMonitors(ref MessageWriter writer, DisplayMonitorState[] monitors)
    {
        var arrayStart = writer.WriteArrayStart(DBusType.Struct);
        foreach (var monitor in monitors)
        {
            var modeId = GetModeId(monitor);

            _logger.LogInformation(
                "GetCurrentState monitor: connector={Connector} vendor={Vendor} product={Product} serial={Serial} geometry={X},{Y} {Width}x{Height} scale={Scale} refresh={RefreshRate} mode={ModeId}",
                monitor.Connector,
                monitor.Vendor,
                monitor.Product,
                monitor.Serial,
                monitor.X,
                monitor.Y,
                monitor.Width,
                monitor.Height,
                monitor.Scale,
                monitor.RefreshRate,
                modeId);

            writer.WriteStructureStart();
            WriteMonitorSpec(ref writer, monitor);
            WriteMonitorModes(ref writer, monitor, modeId);
            writer.WriteDictionary(new KeyValuePair<string, VariantValue>[]
            {
                new("display-name", VariantValue.String(monitor.Connector)),
                new("is-builtin", VariantValue.Bool(false)),
                new("is-for-lease", VariantValue.Bool(false)),
                new("color-mode", VariantValue.UInt32(0)),
                new("rgb-range", VariantValue.UInt32(0))
            });
        }

        writer.WriteArrayEnd(arrayStart);
    }

    private void WriteMonitorModes(ref MessageWriter writer, DisplayMonitorState monitor, string modeId)
    {
        var arrayStart = writer.WriteArrayStart(DBusType.Struct);
        writer.WriteStructureStart();
        writer.WriteString(modeId);
        writer.WriteInt32(monitor.Width);
        writer.WriteInt32(monitor.Height);
        writer.WriteDouble(monitor.RefreshRate);
        writer.WriteDouble(monitor.Scale);
        writer.WriteArray(new[] { monitor.Scale });
        writer.WriteDictionary(new KeyValuePair<string, VariantValue>[]
        {
            new("is-current", VariantValue.Bool(true)),
            new("is-preferred", VariantValue.Bool(true))
        });
        writer.WriteArrayEnd(arrayStart);
    }

    private void WriteLogicalMonitors(ref MessageWriter writer, DisplayMonitorState[] monitors)
    {
        var arrayStart = writer.WriteArrayStart(DBusType.Struct);
        foreach (var monitor in monitors)
        {
            writer.WriteStructureStart();
            writer.WriteInt32(monitor.X);
            writer.WriteInt32(monitor.Y);
            writer.WriteDouble(monitor.Scale);
            writer.WriteUInt32(TransformNormal);
            writer.WriteBool(monitor.Index == 0);

            var monitorArrayStart = writer.WriteArrayStart(DBusType.Struct);
            WriteMonitorSpec(ref writer, monitor);
            writer.WriteArrayEnd(monitorArrayStart);

            writer.WriteDictionary(Array.Empty<KeyValuePair<string, VariantValue>>());

            _logger.LogInformation(
                "GetCurrentState logical monitor: connector={Connector} x={X} y={Y} scale={Scale} primary={Primary}",
                monitor.Connector,
                monitor.X,
                monitor.Y,
                monitor.Scale,
                monitor.Index == 0);
        }

        writer.WriteArrayEnd(arrayStart);
    }

    private static void WriteMonitorSpec(ref MessageWriter writer, DisplayMonitorState monitor)
    {
        writer.WriteStructureStart();
        writer.WriteString(monitor.Connector);
        writer.WriteString(monitor.Vendor);
        writer.WriteString(monitor.Product);
        writer.WriteString(monitor.Serial);
    }

    private static string GetModeId(DisplayMonitorState monitor) =>
        $"{monitor.Width}x{monitor.Height}@{monitor.RefreshRate:0.###}";

    private static void ReplyEmpty(MethodContext context)
    {
        using var writer = context.CreateReplyWriter(null);
        context.Reply(writer.CreateMessage());
    }
}
