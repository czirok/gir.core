namespace St;

public static class Module
{
    private static bool IsInitialized;

    /// <summary>
    /// Initialize the <c>St</c> module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Calling this method is necessary to correctly initialize the bindings
    /// and should be done before using anything else in the <see cref="St" />
    /// namespace.
    /// </para>
    /// <para>
    /// Calling this method will also initialize the modules this module
    /// depends on:
    /// </para>
    /// <list type="table">
    /// <item><description><see cref="Clutter.Module" /></description></item>
    /// <item><description><see cref="Cogl.Module" /></description></item>
    /// <item><description><see cref="GdkPixbuf.Module" /></description></item>
    /// <item><description><see cref="Meta.Module" /></description></item>
    /// </list>
    /// </remarks>
    public static void Initialize()
    {
        if (IsInitialized)
            return;

        Clutter.Module.Initialize();
        Cogl.Module.Initialize();
        GdkPixbuf.Module.Initialize();
        Meta.Module.Initialize();

        Internal.ImportResolver.RegisterAsDllImportResolver();
        Internal.TypeRegistration.RegisterTypes();

        IsInitialized = true;
    }
}
