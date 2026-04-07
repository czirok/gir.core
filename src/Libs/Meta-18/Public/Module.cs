namespace Meta;

public static class Module
{
    private static bool IsInitialized;

    /// <summary>
    /// Initialize the <c>Meta</c> module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Calling this method is necessary to correctly initialize the bindings
    /// and should be done before using anything else in the <see cref="Meta" />
    /// namespace.
    /// </para>
    /// <para>
    /// Calling this method will also initialize the modules this module
    /// depends on:
    /// </para>
    /// <list type="table">
    /// <item><description><see cref="Clutter.Module" /></description></item>
    /// <item><description><see cref="Cogl.Module" /></description></item>
    /// <item><description><see cref="GDesktopEnums.Module" /></description></item>
    /// <item><description><see cref="GObject.Module" /></description></item>
    /// <item><description><see cref="Gio.Module" /></description></item>
    /// <item><description><see cref="Mtk.Module" /></description></item>
    /// </list>
    /// </remarks>
    public static void Initialize()
    {
        if (IsInitialized)
            return;

        Clutter.Module.Initialize();
        Cogl.Module.Initialize();
        GDesktopEnums.Module.Initialize();
        GObject.Module.Initialize();
        Gio.Module.Initialize();
        Mtk.Module.Initialize();

        Internal.ImportResolver.RegisterAsDllImportResolver();
        Internal.TypeRegistration.RegisterTypes();

        IsInitialized = true;
    }
}
