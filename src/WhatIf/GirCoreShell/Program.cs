using System.Runtime.Versioning;
using GirCoreLauncher.Services;
using GirCoreShell.DBus;
using GirCoreShell.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tmds.Systemd;

[assembly: UnsupportedOSPlatform("Windows")]
[assembly: UnsupportedOSPlatform("macOS")]

GLib.Module.Initialize();
GioUnix.Module.Initialize();
Meta.Module.Initialize();
St.Module.Initialize();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(static logging => logging.AddJournal())
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton(new ShellCommandLine(args));
        services.AddSingleton<ShellRuntimeState>();
        services.AddSingleton<GirCoreLauncherCatalog>();

        // DBus infrastructure.
        services.AddSingleton<SessionBusConnection>();
        services.AddHostedService(static sp => sp.GetRequiredService<SessionBusConnection>());

        services.AddSingleton<GnomeShellServer>();
        services.AddSingleton<GnomeShellBrightnessServer>();
        services.AddSingleton<GnomeMutterDisplayConfigServer>();
        services.AddSingleton<GnomeMutterServiceChannelServer>();
        services.AddSingleton<GnomeShellIntrospectServer>();
        services.AddSingleton<GnomeScreenSaverServer>();
        services.AddHostedService<ShellDbusHostService>();

        // Register as singleton and hosted service to guarantee one shared runtime instance.
        services.AddSingleton<ShellRuntimeService>();
        services.AddHostedService(static sp => sp.GetRequiredService<ShellRuntimeService>());
    })
    .Build();

var startupLogger = host.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("GirCoreShell.Startup");

startupLogger.LogInformation("Host built. Starting GirCoreShell runtime.");

await host.RunAsync();
