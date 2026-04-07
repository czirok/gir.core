namespace Gck;

public static class GckModule
{
    private static bool IsInitialized;

    /// <summary>
    /// Initialize the <c>Gck</c> module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Calling this method is necessary to correctly initialize the bindings
    /// and should be done before using anything else in the <see cref="Gck" />
    /// namespace.
    /// </para>
    /// <para>
    /// Calling this method will also initialize the modules this module
    /// depends on:
    /// </para>
    /// <list type="table">
    /// <item><description><see cref="GObject.Module" /></description></item>
    /// <item><description><see cref="Gio.Module" /></description></item>
    /// <item><description><see cref="P11Kit.Module" /></description></item>
    /// </list>
    /// </remarks>
    public static void Initialize()
    {
        if (IsInitialized)
            return;

        GObject.Module.Initialize();
        Gio.Module.Initialize();
        P11Kit.Module.Initialize();

        Internal.ImportResolver.RegisterAsDllImportResolver();
        Internal.TypeRegistration.RegisterTypes();

        IsInitialized = true;
    }
}
