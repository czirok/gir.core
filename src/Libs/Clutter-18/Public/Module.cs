namespace Clutter;

public static class Module
{
    private static bool IsInitialized;

    /// <summary>
    /// Initialize the <c>Clutter</c> module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Calling this method is necessary to correctly initialize the bindings
    /// and should be done before using anything else in the <see cref="Clutter" />
    /// namespace.
    /// </para>
    /// <para>
    /// Calling this method will also initialize the modules this module
    /// depends on:
    /// </para>
    /// <list type="table">
    /// <item><description><see cref="Atk.Module" /></description></item>
    /// <item><description><see cref="Cogl.Module" /></description></item>
    /// <item><description><see cref="GObject.Module" /></description></item>
    /// <item><description><see cref="Gio.Module" /></description></item>
    /// <item><description><see cref="Mtk.Module" /></description></item>
    /// <item><description><see cref="Pango.Module" /></description></item>
    /// </list>
    /// </remarks>
    public static void Initialize()
    {
        if (IsInitialized)
            return;

        Atk.Module.Initialize();
        Cogl.Module.Initialize();
        GObject.Module.Initialize();
        Gio.Module.Initialize();
        Mtk.Module.Initialize();
        Pango.Module.Initialize();

        Internal.ImportResolver.RegisterAsDllImportResolver();
        Internal.TypeRegistration.RegisterTypes();

        IsInitialized = true;
    }
}
