using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace GirCoreMutter;

[UnsupportedOSPlatform("Windows")]
[UnsupportedOSPlatform("macOS")]
public class MutterPlugin : Meta.Plugin, GObject.GTypeProvider, GObject.InstanceFactory
{
    static readonly GObject.Internal.ClassInitFunc ClassInit = OnClassInit;
    static readonly GObject.Internal.InstanceInitFunc InstanceInit = static (_, _) => { };
    static readonly Meta.Internal.PluginClassData.StartCallback StartCallback = OnStart;
    static readonly GObject.Type RegisteredType = RegisterType();

    public static Clutter.Stage? Stage { get; private set; }

    protected internal MutterPlugin(Meta.Internal.PluginHandle handle)
        : base(handle)
    {
    }

    public static new GObject.Type GetGType() => RegisteredType;

    public static object Create(IntPtr handle, bool ownsHandle)
    {
        var plugin = new MutterPlugin(new Meta.Internal.PluginHandle(handle));
        GObject.Internal.InstanceCache.AddToggleRef(plugin);
        return plugin;
    }

    static void OnClassInit(IntPtr gClass, IntPtr classData)
    {
        var pluginClass = Marshal.PtrToStructure<Meta.Internal.PluginClassData>(gClass);
        pluginClass.Start = StartCallback;
        Marshal.StructureToPtr(pluginClass, gClass, false);
    }

    static void OnStart(IntPtr pluginPtr)
    {
        var plugin = (Meta.Plugin)GObject.Internal.InstanceWrapper.WrapHandle<Meta.Plugin>(pluginPtr, false);
        var display = plugin.GetDisplay();
        var compositor = display.GetCompositor();

        // This is the place where plugin startup logic can access and configure the stage.
        Stage = compositor.GetStage();
        Stage.Show();
    }

    static GObject.Type RegisterType()
    {
        GObject.Functions.TypeQuery(Meta.Plugin.GetGType(), out var query);

        using var typeName = GLib.Internal.NonNullableUtf8StringOwnedHandle.Create("DotnetMutterPlugin");
        var typeId = GObject.Internal.Functions.TypeRegisterStaticSimple(
            Meta.Plugin.GetGType(),
            typeName,
            query.Handle.GetClassSize(),
            ClassInit,
            query.Handle.GetInstanceSize(),
            InstanceInit,
            GObject.TypeFlags.None);

        if (typeId == 0)
            throw new InvalidOperationException("Failed to register DotnetMutterPlugin GType.");

        var type = new GObject.Type(typeId);
        GObject.Internal.DynamicInstanceFactory.Register(type, Create);
        return type;
    }
}
