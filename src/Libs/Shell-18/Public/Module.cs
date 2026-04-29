using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Shell;

public static class Module
{
    private static bool IsInitialized;
    private static DllImportResolver? CustomDllImportResolver;

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

        NativeLibrary.SetDllImportResolver(typeof(Module).Assembly, CustomDllImportResolver ?? Internal.ImportResolver.Resolve);
        Internal.TypeRegistration.RegisterTypes();

        IsInitialized = true;
    }

    /// <summary>
    /// Set a custom DllImportResolver. This disables the automatic loading of native binaries for
    /// Shell. If the given DllImportResolver receives the library name "Shell" it has to return a pointer
    /// to the desired native Shell binary.
    /// </summary>
    /// <remarks>
    /// Please be aware that using this API means you are out of the officially supported area
    /// as you are able to combine GirCore with some binary the package was not build for. Please consider
    /// to generate a custom GirCore package which exactly matches your binary.
    /// </remarks>
    /// <param name="customDllImportResolver">Custom DllImportResolver to use.</param>
    /// <exception cref="Exception">Throws an exception if the method is called after module initialization.</exception>
    [Experimental("GirCore1009", UrlFormat = "https://gircore.github.io/docs/integration/diagnostic/1009.html")]
    public static void SetCustomDllImportResolver(DllImportResolver customDllImportResolver)
    {
        if (IsInitialized)
            throw new Exception("Can't set a custom DllImportResolver after initialization is done.");

        CustomDllImportResolver = customDllImportResolver;
    }
}
