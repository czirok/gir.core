using System.Runtime.Versioning;

[UnsupportedOSPlatform("Windows")]
[UnsupportedOSPlatform("macOS")]
class Program
{
    static void Main(string[] args)
    {
        Meta.Module.Initialize();
        Shell.Module.Initialize();

        var context = Meta.Functions.CreateContext("GirCore Mutter .NET");
        context.SetPluginGtype(GirCoreMutter.MutterPlugin.GetGType());

        string[]? argv = args;
        context.Configure(ref argv);        // meta_context_configure()
        context.Setup();                    // meta_context_setup()
        context.Start();                    // meta_context_start() — start wyaland here
        context.NotifyReady();              // meta_context_notify_ready()
        context.RunMainLoop();              // meta_context_run_main_loop()        
    }
}
