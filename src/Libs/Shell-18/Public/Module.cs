namespace Shell;

public static class Module
{
    private static bool IsInitialized;

    /// <summary>
    /// Initialize the <c>Shell</c> module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Calling this method is necessary to correctly initialize the bindings
    /// and should be done before using anything else in the <see cref="Shell" />
    /// namespace.
    /// </para>
    /// <para>
    /// Calling this method will also initialize the modules this module
    /// depends on:
    /// </para>
    /// <list type="table">
    /// <item><description><see cref="Clutter.Module" /></description></item>
    /// <item><description><see cref="Gcr.GcrModule" /></description></item>
    /// <item><description><see cref="GdkPixbuf.Module" /></description></item>
    /// <item><description><see cref="GioUnix.Module" /></description></item
    /// <item><description><see cref="Gvc.Module" /></description></item>
    /// <item><description><see cref="Meta.Module" /></description></item>
    /// <item><description><see cref="NM.Module" /></description></item>
    /// <item><description><see cref="PolkitAgent.Module" /></description></item>
    /// <item><description><see cref="St.Module" /></description></item>
    /// </list>
    /// </remarks>
    public static void Initialize()
    {
        if (IsInitialized)
            return;

        Clutter.Module.Initialize();
        Gcr.GcrModule.Initialize();
        GdkPixbuf.Module.Initialize();
        GioUnix.Module.Initialize();
        Gvc.Module.Initialize();
        Meta.Module.Initialize();
        NM.Module.Initialize();
        PolkitAgent.Module.Initialize();
        St.Module.Initialize();

        Internal.ImportResolver.RegisterAsDllImportResolver();
        Internal.TypeRegistration.RegisterTypes();

        IsInitialized = true;
    }
}
