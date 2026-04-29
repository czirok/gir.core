using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using GirCoreLauncher;
using GirCoreLauncher.Services;
using GirCoreShell.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GirCoreShell;

[UnsupportedOSPlatform("Windows")]
[UnsupportedOSPlatform("macOS")]
public class GnomeShellPlugin : Meta.Plugin, GObject.GTypeProvider, GObject.InstanceFactory
{
    private static readonly GObject.Internal.ClassInitFunc ClassInit = OnClassInit;
    private static readonly GObject.Internal.InstanceInitFunc InstanceInit = static (_, _) => { };
    private static readonly Meta.Internal.PluginClassData.StartCallback StartCallback = OnStart;
    private static readonly Meta.Internal.PluginClassData.MinimizeCallback MinimizeCallback = OnMinimize;
    private static readonly Meta.Internal.PluginClassData.UnminimizeCallback UnminimizeCallback = OnUnminimize;
    private static readonly Meta.Internal.PluginClassData.SizeChangedCallback SizeChangedCallback = OnSizeChanged;
    private static readonly Meta.Internal.PluginClassData.SizeChangeCallback SizeChangeCallback = OnSizeChange;
    private static readonly Meta.Internal.PluginClassData.MapCallback MapCallback = OnMap;
    private static readonly Meta.Internal.PluginClassData.DestroyCallback DestroyCallback = OnDestroy;
    private static readonly Meta.Internal.PluginClassData.SwitchWorkspaceCallback SwitchWorkspaceCallback = OnSwitchWorkspace;
    private static readonly Meta.Internal.PluginClassData.ShowTilePreviewCallback ShowTilePreviewCallback = OnShowTilePreview;
    private static readonly Meta.Internal.PluginClassData.HideTilePreviewCallback HideTilePreviewCallback = OnHideTilePreview;
    private static readonly Meta.Internal.PluginClassData.ShowWindowMenuCallback ShowWindowMenuCallback = OnShowWindowMenu;
    private static readonly Meta.Internal.PluginClassData.ShowWindowMenuForRectCallback ShowWindowMenuForRectCallback = OnShowWindowMenuForRect;
    private static readonly Meta.Internal.PluginClassData.KillWindowEffectsCallback KillWindowEffectsCallback = OnKillWindowEffects;
    private static readonly Meta.Internal.PluginClassData.KillSwitchWorkspaceCallback KillSwitchWorkspaceCallback = OnKillSwitchWorkspace;
    private static readonly Meta.Internal.PluginClassData.KeybindingFilterCallback KeybindingFilterCallback = OnKeybindingFilter;
    private static readonly Meta.Internal.PluginClassData.ConfirmDisplayChangeCallback ConfirmDisplayChangeCallback = OnConfirmDisplayChange;
    private static readonly Meta.Internal.PluginClassData.CreateCloseDialogCallback CreateCloseDialogCallback = OnCreateCloseDialog;
    private static readonly Meta.Internal.PluginClassData.CreateInhibitShortcutsDialogCallback CreateInhibitShortcutsDialogCallback = OnCreateInhibitShortcutsDialog;
    private static readonly Meta.Internal.PluginClassData.LocatePointerCallback LocatePointerCallback = OnLocatePointer;

    private static readonly GObject.Type RegisteredType = RegisterType();
    private static IServiceProvider _services = default!;
    private static ILogger<GnomeShellPlugin>? _logger;
    private static ShellRuntimeState? _runtimeState;
    private static Clutter.Actor? _backgroundActor;
    private static GirCoreBar? _bar;
    private static readonly object EffectStateLock = new();
    private static readonly HashSet<nint> MinimizePending = [];
    private static readonly HashSet<nint> UnminimizePending = [];
    private static readonly HashSet<nint> MapPending = [];
    private static readonly HashSet<nint> DestroyPending = [];
    private static readonly HashSet<nint> SizeChangePending = [];
    private static bool _switchWorkspacePending;

    protected internal GnomeShellPlugin(Meta.Internal.PluginHandle handle)
        : base(handle)
    {
        _logger = _services.GetRequiredService<ILogger<GnomeShellPlugin>>();
        _logger.LogInformation("GnomeShellPlugin instance created.");
    }

    public static GObject.Type GetGType(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        _services = services;
        _runtimeState = services.GetRequiredService<ShellRuntimeState>();
        return RegisteredType;
    }

    public static object Create(IntPtr handle, bool ownsHandle)
    {
        var plugin = new GnomeShellPlugin(new Meta.Internal.PluginHandle(handle));
        GObject.Internal.InstanceCache.AddToggleRef(plugin);
        return plugin;
    }

    private static void OnClassInit(IntPtr gClass, IntPtr classData)
    {
        var pluginClass = Marshal.PtrToStructure<Meta.Internal.PluginClassData>(gClass);
        pluginClass.Start = StartCallback;
        pluginClass.Minimize = MinimizeCallback;
        pluginClass.Unminimize = UnminimizeCallback;
        pluginClass.SizeChanged = SizeChangedCallback;
        pluginClass.SizeChange = SizeChangeCallback;
        pluginClass.Map = MapCallback;
        pluginClass.Destroy = DestroyCallback;
        pluginClass.SwitchWorkspace = SwitchWorkspaceCallback;
        pluginClass.ShowTilePreview = ShowTilePreviewCallback;
        pluginClass.HideTilePreview = HideTilePreviewCallback;
        pluginClass.ShowWindowMenu = ShowWindowMenuCallback;
        pluginClass.ShowWindowMenuForRect = ShowWindowMenuForRectCallback;
        pluginClass.KillWindowEffects = KillWindowEffectsCallback;
        pluginClass.KillSwitchWorkspace = KillSwitchWorkspaceCallback;
        pluginClass.KeybindingFilter = KeybindingFilterCallback;
        pluginClass.ConfirmDisplayChange = ConfirmDisplayChangeCallback;
        pluginClass.CreateCloseDialog = CreateCloseDialogCallback;
        pluginClass.CreateInhibitShortcutsDialog = CreateInhibitShortcutsDialogCallback;
        pluginClass.LocatePointer = LocatePointerCallback;
        Marshal.StructureToPtr(pluginClass, gClass, false);
    }

    private static void OnStart(IntPtr pluginPtr)
    {
        try
        {
            _logger?.LogInformation("[Plugin.Start] Initializing stage and setting background color #68217a.");

            var plugin = (Meta.Plugin)GObject.Internal.InstanceWrapper.WrapHandle<Meta.Plugin>(pluginPtr, false);
            var display = plugin.GetDisplay();
            var compositor = display.GetCompositor();
            var stage = compositor.GetStage();

            UpdateDisplayConfiguration(display, stage);

            if (Cogl.Color.FromString(out var gircolor, "#68217a"))
            {
                stage.SetBackgroundColor(gircolor);
                EnsureBackgroundActor(stage, gircolor);
                _logger?.LogInformation("[Plugin.Start] Requested stage background color: #68217a.");
            }
            else
            {
                _logger?.LogWarning("[Plugin.Start] Failed to parse color #68217a, stage background unchanged.");
            }

            EnsureMiniShellUi(stage);

            stage.Show();
            stage.QueueRedraw();
            _runtimeState?.SetShellReady(true);
            _logger?.LogInformation("[Plugin.Start] Stage is visible.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[Plugin.Start] Failed while setting up stage.");
            throw;
        }
    }

    private static void EnsureBackgroundActor(Clutter.Stage stage, Cogl.Color color)
    {
        var width = 0;
        var height = 0;
        var snapshot = _runtimeState?.GetDisplayConfiguration();
        if (snapshot is not null)
        {
            foreach (var monitor in snapshot.Monitors)
            {
                width = Math.Max(width, monitor.X + monitor.Width);
                height = Math.Max(height, monitor.Y + monitor.Height);
            }
        }

        if (width <= 0 || height <= 0)
        {
            stage.GetSize(out var stageWidth, out var stageHeight);
            width = stageWidth > 0 ? (int)stageWidth : DisplayMonitorState.Fallback.Width;
            height = stageHeight > 0 ? (int)stageHeight : DisplayMonitorState.Fallback.Height;
            _logger?.LogWarning(
                "[Plugin.Start] Background actor using fallback stage size={Width}x{Height}.",
                width,
                height);
        }

        _backgroundActor ??= Clutter.Actor.New();
        _backgroundActor.SetName("GirCoreShellBackground");
        _backgroundActor.SetPosition(0, 0);
        _backgroundActor.SetSize(width, height);
        _backgroundActor.SetClip(0, 0, width, height);
        _backgroundActor.SetClipToAllocation(true);
        _backgroundActor.SetReactive(false);
        _backgroundActor.SetOpacity(255);
        _backgroundActor.SetBackgroundColor(color);
        _backgroundActor.Show();

        if (_backgroundActor.GetParent() is null)
        {
            stage.InsertChildAtIndex(_backgroundActor, 0);
            _logger?.LogInformation(
                "[Plugin.Start] Background actor inserted at stage index 0. size={Width}x{Height}",
                width,
                height);
        }
        else
        {
            _logger?.LogInformation(
                "[Plugin.Start] Background actor updated. size={Width}x{Height}",
                width,
                height);
        }

        _backgroundActor.QueueRedraw();
        stage.QueueRedraw();
    }

    private static void EnsureMiniShellUi(Clutter.Stage stage)
    {
        try
        {
            if (_services is null)
            {
                _logger?.LogWarning("[Plugin.Start] Mini launcher skipped: service provider is unavailable.");
                return;
            }

            var appCatalog = _services.GetRequiredService<GirCoreLauncherCatalog>();
            var uiLogger = _services.GetRequiredService<ILogger<GirCoreBar>>();
            _bar ??= new GirCoreBar(stage, appCatalog, uiLogger);
            _bar.EnsureVisible();
            _logger?.LogInformation("[Plugin.Start] Mini shell launcher requested.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[Plugin.Start] Failed to initialize mini shell launcher.");
        }
    }

    private static void UpdateDisplayConfiguration(Meta.Display display, Clutter.Stage stage)
    {
        try
        {
            var monitorCount = display.GetNMonitors();
            display.GetSize(out var displayWidth, out var displayHeight);
            _logger?.LogInformation(
                "[Plugin.Start] Display snapshot source=Meta.Display monitors={MonitorCount} size={Width}x{Height}.",
                monitorCount,
                displayWidth,
                displayHeight);

            var monitors = new DisplayMonitorState[Math.Max(monitorCount, 0)];
            for (var i = 0; i < monitors.Length; i++)
            {
                display.GetMonitorGeometry(i, out var geometry);
                var scale = display.GetMonitorScale(i);
                var width = geometry.Width > 0 ? geometry.Width : displayWidth;
                var height = geometry.Height > 0 ? geometry.Height : displayHeight;

                monitors[i] = new DisplayMonitorState(
                    Index: i,
                    Connector: $"MONITOR-{i}",
                    Vendor: "mutter",
                    Product: "display",
                    Serial: i.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    X: geometry.X,
                    Y: geometry.Y,
                    Width: width > 0 ? width : DisplayMonitorState.Fallback.Width,
                    Height: height > 0 ? height : DisplayMonitorState.Fallback.Height,
                    Scale: scale > 0 ? scale : 1.0,
                    RefreshRate: 60.0);

                _logger?.LogInformation(
                    "[Plugin.Start] Monitor snapshot index={Index} connector={Connector} geometry={X},{Y} {Width}x{Height} scale={Scale}.",
                    monitors[i].Index,
                    monitors[i].Connector,
                    monitors[i].X,
                    monitors[i].Y,
                    monitors[i].Width,
                    monitors[i].Height,
                    monitors[i].Scale);
            }

            if (monitors.Length == 0)
            {
                stage.GetSize(out var stageWidth, out var stageHeight);
                _logger?.LogWarning(
                    "[Plugin.Start] Meta.Display returned no monitors; using stage fallback size={Width}x{Height}.",
                    stageWidth,
                    stageHeight);

                monitors =
                [
                    DisplayMonitorState.Fallback with
                    {
                        Width = stageWidth > 0 ? (int) stageWidth : DisplayMonitorState.Fallback.Width,
                        Height = stageHeight > 0 ? (int) stageHeight : DisplayMonitorState.Fallback.Height
                    }
                ];
            }

            var snapshot = _runtimeState?.UpdateDisplayConfiguration(monitors);
            if (snapshot is not null)
            {
                _logger?.LogInformation(
                    "[Plugin.Start] Runtime display snapshot updated. serial={Serial} monitors={MonitorCount}.",
                    snapshot.Serial,
                    snapshot.Monitors.Length);
            }
            else
            {
                _logger?.LogWarning("[Plugin.Start] Runtime state is unavailable; display snapshot was not stored.");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[Plugin.Start] Failed to update display configuration snapshot.");
        }
    }

    private static void OnMinimize(IntPtr pluginPtr, IntPtr actorPtr)
    {
        BeginEffect(MinimizePending, actorPtr);

        var plugin = (Meta.Plugin)GObject.Internal.InstanceWrapper.WrapHandle<Meta.Plugin>(pluginPtr, false);
        var actor = (Meta.WindowActor)GObject.Internal.InstanceWrapper.WrapHandle<Meta.WindowActor>(actorPtr, false);
        CompleteEffect(MinimizePending, actorPtr, () => plugin.MinimizeCompleted(actor));
    }

    private static void OnUnminimize(IntPtr pluginPtr, IntPtr actorPtr)
    {
        BeginEffect(UnminimizePending, actorPtr);

        var plugin = (Meta.Plugin)GObject.Internal.InstanceWrapper.WrapHandle<Meta.Plugin>(pluginPtr, false);
        var actor = (Meta.WindowActor)GObject.Internal.InstanceWrapper.WrapHandle<Meta.WindowActor>(actorPtr, false);
        CompleteEffect(UnminimizePending, actorPtr, () => plugin.UnminimizeCompleted(actor));
    }

    private static void OnSizeChanged(IntPtr pluginPtr, IntPtr actorPtr)
    {
        _logger?.LogDebug("[Plugin.SizeChanged] Window size-changed event.");
    }

    private static void OnSizeChange(IntPtr pluginPtr, IntPtr actorPtr, Meta.SizeChange whichChange, IntPtr oldFrameRectPtr, IntPtr oldBufferRectPtr)
    {
        BeginEffect(SizeChangePending, actorPtr);

        var plugin = (Meta.Plugin)GObject.Internal.InstanceWrapper.WrapHandle<Meta.Plugin>(pluginPtr, false);
        var actor = (Meta.WindowActor)GObject.Internal.InstanceWrapper.WrapHandle<Meta.WindowActor>(actorPtr, false);
        CompleteEffect(SizeChangePending, actorPtr, () => plugin.SizeChangeCompleted(actor));
    }

    private static void OnMap(IntPtr pluginPtr, IntPtr actorPtr)
    {
        BeginEffect(MapPending, actorPtr);

        var plugin = (Meta.Plugin)GObject.Internal.InstanceWrapper.WrapHandle<Meta.Plugin>(pluginPtr, false);
        var actor = (Meta.WindowActor)GObject.Internal.InstanceWrapper.WrapHandle<Meta.WindowActor>(actorPtr, false);
        UpdateWindowSnapshot(actor, "Map");
        CompleteEffect(MapPending, actorPtr, () => plugin.MapCompleted(actor));
    }

    private static void OnDestroy(IntPtr pluginPtr, IntPtr actorPtr)
    {
        BeginEffect(DestroyPending, actorPtr);

        var plugin = (Meta.Plugin)GObject.Internal.InstanceWrapper.WrapHandle<Meta.Plugin>(pluginPtr, false);
        var actor = (Meta.WindowActor)GObject.Internal.InstanceWrapper.WrapHandle<Meta.WindowActor>(actorPtr, false);
        RemoveWindowSnapshot(actor, "Destroy");
        CompleteEffect(DestroyPending, actorPtr, () => plugin.DestroyCompleted(actor));
    }

    private static void OnSwitchWorkspace(IntPtr pluginPtr, int from, int to, Meta.MotionDirection direction)
    {
        lock (EffectStateLock)
        {
            _switchWorkspacePending = true;
        }

        var plugin = (Meta.Plugin)GObject.Internal.InstanceWrapper.WrapHandle<Meta.Plugin>(pluginPtr, false);
        plugin.SwitchWorkspaceCompleted();

        lock (EffectStateLock)
        {
            _switchWorkspacePending = false;
        }
    }

    private static void OnShowTilePreview(IntPtr pluginPtr, IntPtr windowPtr, IntPtr tileRectPtr, int tileMonitorNumber)
    {
        _logger?.LogDebug("[Plugin.ShowTilePreview] monitor={Monitor}", tileMonitorNumber);
    }

    private static void OnHideTilePreview(IntPtr pluginPtr)
    {
        _logger?.LogDebug("[Plugin.HideTilePreview]");
    }

    private static void OnShowWindowMenu(IntPtr pluginPtr, IntPtr windowPtr, Meta.WindowMenuType menu, int x, int y)
    {
        _logger?.LogDebug("[Plugin.ShowWindowMenu] type={MenuType} at {X},{Y}", menu, x, y);
    }

    private static void OnShowWindowMenuForRect(IntPtr pluginPtr, IntPtr windowPtr, Meta.WindowMenuType menu, IntPtr rectPtr)
    {
        _logger?.LogDebug("[Plugin.ShowWindowMenuForRect] type={MenuType}", menu);
    }

    private static void OnKillWindowEffects(IntPtr pluginPtr, IntPtr actorPtr)
    {
        var plugin = (Meta.Plugin)GObject.Internal.InstanceWrapper.WrapHandle<Meta.Plugin>(pluginPtr, false);
        var actor = (Meta.WindowActor)GObject.Internal.InstanceWrapper.WrapHandle<Meta.WindowActor>(actorPtr, false);

        CompleteIfPending(MinimizePending, actorPtr, () => plugin.MinimizeCompleted(actor));
        CompleteIfPending(UnminimizePending, actorPtr, () => plugin.UnminimizeCompleted(actor));
        CompleteIfPending(MapPending, actorPtr, () => plugin.MapCompleted(actor));
        CompleteIfPending(DestroyPending, actorPtr, () => plugin.DestroyCompleted(actor));
        CompleteIfPending(SizeChangePending, actorPtr, () => plugin.SizeChangeCompleted(actor));
    }

    private static void OnKillSwitchWorkspace(IntPtr pluginPtr)
    {
        var plugin = (Meta.Plugin)GObject.Internal.InstanceWrapper.WrapHandle<Meta.Plugin>(pluginPtr, false);

        bool shouldComplete;
        lock (EffectStateLock)
        {
            shouldComplete = _switchWorkspacePending;
            _switchWorkspacePending = false;
        }

        if (shouldComplete)
            plugin.SwitchWorkspaceCompleted();
    }

    private static bool OnKeybindingFilter(IntPtr pluginPtr, IntPtr bindingPtr)
    {
        return false;
    }

    private static void OnConfirmDisplayChange(IntPtr pluginPtr)
    {
        var plugin = (Meta.Plugin)GObject.Internal.InstanceWrapper.WrapHandle<Meta.Plugin>(pluginPtr, false);
        plugin.CompleteDisplayChange(true);
    }

    private static IntPtr OnCreateCloseDialog(IntPtr pluginPtr, IntPtr windowPtr)
    {
        return IntPtr.Zero;
    }

    private static IntPtr OnCreateInhibitShortcutsDialog(IntPtr pluginPtr, IntPtr windowPtr)
    {
        return IntPtr.Zero;
    }

    private static void OnLocatePointer(IntPtr pluginPtr)
    {
        _logger?.LogDebug("[Plugin.LocatePointer]");
    }

    private static GObject.Type RegisterType()
    {
        GObject.Functions.TypeQuery(Meta.Plugin.GetGType(), out var query);

        using var typeName = GLib.Internal.NonNullableUtf8StringOwnedHandle.Create("DotnetGnomeShellPlugin");
        var typeId = GObject.Internal.Functions.TypeRegisterStaticSimple(
            Meta.Plugin.GetGType(),
            typeName,
            query.Handle.GetClassSize(),
            ClassInit,
            query.Handle.GetInstanceSize(),
            InstanceInit,
            GObject.TypeFlags.None);

        if (typeId == 0)
            throw new InvalidOperationException("Failed to register DotnetGnomeShellPlugin GType.");

        var type = new GObject.Type(typeId);
        GObject.Internal.DynamicInstanceFactory.Register(type, Create);
        return type;
    }

    private static void UpdateWindowSnapshot(Meta.WindowActor actor, string source)
    {
        try
        {
            var window = actor.GetMetaWindow();
            if (window is null)
            {
                _logger?.LogWarning("[Plugin.{Source}] WindowActor has no Meta.Window.", source);
                return;
            }

            if (!IsEligibleForIntrospection(window))
            {
                _logger?.LogInformation(
                    "[Plugin.{Source}] Window skipped for introspection. id={WindowId} type={WindowType} override_redirect={OverrideRedirect}",
                    source,
                    window.GetId(),
                    window.GetWindowType(),
                    window.IsOverrideRedirect());
                return;
            }

            window.GetFrameRect(out var rect);
            var appId = GetAppId(window);
            var snapshot = new WindowIntrospectionState(
                Id: window.GetId(),
                AppId: appId,
                Title: NullIfEmpty(window.GetTitle()),
                WmClass: NullIfEmpty(window.GetWmClass()),
                ClientType: (uint)window.GetClientType(),
                IsHidden: window.IsHidden(),
                HasFocus: window.HasFocus(),
                Width: rect.Width > 0 ? (uint)rect.Width : 0,
                Height: rect.Height > 0 ? (uint)rect.Height : 0,
                SandboxedAppId: NullIfEmpty(window.GetSandboxedAppId()));

            _runtimeState?.UpsertWindow(snapshot);
            _logger?.LogInformation(
                "[Plugin.{Source}] Window introspection snapshot updated. id={WindowId} app_id={AppId} title={Title} size={Width}x{Height} focus={HasFocus}",
                source,
                snapshot.Id,
                snapshot.AppId,
                snapshot.Title ?? "<none>",
                snapshot.Width,
                snapshot.Height,
                snapshot.HasFocus);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[Plugin.{Source}] Failed to update window introspection snapshot.", source);
        }
    }

    private static void RemoveWindowSnapshot(Meta.WindowActor actor, string source)
    {
        try
        {
            var window = actor.GetMetaWindow();
            if (window is null)
            {
                _logger?.LogWarning("[Plugin.{Source}] Destroyed WindowActor has no Meta.Window.", source);
                return;
            }

            var id = window.GetId();
            _runtimeState?.RemoveWindow(id);
            _logger?.LogInformation("[Plugin.{Source}] Window introspection snapshot removed. id={WindowId}", source, id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[Plugin.{Source}] Failed to remove window introspection snapshot.", source);
        }
    }

    private static bool IsEligibleForIntrospection(Meta.Window window)
    {
        if (window.IsOverrideRedirect())
            return false;

        return window.GetWindowType() is Meta.WindowType.Normal
            or Meta.WindowType.Dialog
            or Meta.WindowType.ModalDialog
            or Meta.WindowType.Utility;
    }

    private static string GetAppId(Meta.Window window)
    {
        return NullIfEmpty(window.GetGtkApplicationId())
            ?? NullIfEmpty(window.GetSandboxedAppId())
            ?? NullIfEmpty(window.GetWmClass())
            ?? $"pid-{window.GetPid()}";
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static void BeginEffect(HashSet<nint> pendingSet, nint actorPtr)
    {
        if (actorPtr == nint.Zero)
            return;

        lock (EffectStateLock)
        {
            pendingSet.Add(actorPtr);
        }
    }

    private static void CompleteEffect(HashSet<nint> pendingSet, nint actorPtr, Action complete)
    {
        try
        {
            complete();
        }
        finally
        {
            if (actorPtr != nint.Zero)
            {
                lock (EffectStateLock)
                {
                    pendingSet.Remove(actorPtr);
                }
            }
        }
    }

    private static void CompleteIfPending(HashSet<nint> pendingSet, nint actorPtr, Action complete)
    {
        if (actorPtr == nint.Zero)
            return;

        bool shouldComplete;
        lock (EffectStateLock)
        {
            shouldComplete = pendingSet.Remove(actorPtr);
        }

        if (shouldComplete)
            complete();
    }

}
