using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tmds.Systemd;

namespace GirCoreShell.Services;

internal sealed record ShellCommandLine(string[] Args);

/// <summary>
/// Owns MetaContext lifecycle for the shell process.
/// This service is intentionally DI-first and keeps all runtime wiring in one place.
/// </summary>
internal sealed class ShellRuntimeService : IHostedService
{
    private readonly ShellCommandLine _commandLine;
    private readonly IServiceProvider _services;
    private readonly ShellRuntimeState _runtimeState;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ShellRuntimeService> _logger;

    private Meta.Context? _context;
    private Thread? _runtimeThread;
    private TaskCompletionSource? _startedTcs;

    public ShellRuntimeService(
        ShellCommandLine commandLine,
        IServiceProvider services,
        ShellRuntimeState runtimeState,
        IHostApplicationLifetime lifetime,
        ILogger<ShellRuntimeService> logger)
    {
        _commandLine = commandLine;
        _services = services;
        _runtimeState = runtimeState;
        _lifetime = lifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting MetaContext runtime.");
        _runtimeState.SetShellReady(false);

        _startedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _runtimeThread = new Thread(RunRuntimeThread)
        {
            IsBackground = false,
            Name = "GirCoreShell MetaContext"
        };
        _runtimeThread.Start();

        return _startedTcs.Task.WaitAsync(cancellationToken);
    }

    private void RunRuntimeThread()
    {
        try
        {
            _logger.LogInformation(
                "MetaContext runtime thread started. managed_thread_id={ThreadId}",
                Environment.CurrentManagedThreadId);

            _context = Meta.Functions.CreateContext("GirCore Shell");
            _logger.LogInformation("MetaContext created on runtime thread.");
            _runtimeState.SetMetaContext(_context, Environment.CurrentManagedThreadId);
            _logger.LogInformation(
                "MetaContext published to runtime state. managed_thread_id={ThreadId}",
                Environment.CurrentManagedThreadId);

            _context.SetPluginGtype(GnomeShellPlugin.GetGType(_services));
            _logger.LogInformation("MetaContext plugin GType configured.");

            string[]? argv = _commandLine.Args;
            _logger.LogInformation("Calling MetaContext.Configure on runtime thread. argc={ArgCount}", argv?.Length ?? 0);
            if (!_context.Configure(ref argv))
                throw new InvalidOperationException("MetaContext.Configure failed.");
            _logger.LogInformation("MetaContext.Configure completed.");

            _logger.LogInformation("Calling MetaContext.Setup on runtime thread.");
            if (!_context.Setup())
                throw new InvalidOperationException("MetaContext.Setup failed.");
            _logger.LogInformation("MetaContext.Setup completed.");

            _logger.LogInformation("Calling MetaContext.Start on runtime thread.");
            if (!_context.Start())
                throw new InvalidOperationException("MetaContext.Start failed.");
            _logger.LogInformation("MetaContext.Start completed.");

            _context.NotifyReady();
            _logger.LogInformation("MetaContext.NotifyReady completed.");

            // Notify systemd that the service is ready (Type=notify).
            ServiceManager.Notify(ServiceState.Ready);
            _logger.LogInformation("Notified systemd: READY=1");

            _startedTcs?.TrySetResult();

            _logger.LogInformation(
                "Entering MetaContext main loop on runtime thread. managed_thread_id={ThreadId}",
                Environment.CurrentManagedThreadId);

            var ok = _context.RunMainLoop();
            if (!ok)
            {
                _logger.LogError("MetaContext.RunMainLoop exited with error.");
            }
            else
            {
                _logger.LogInformation("MetaContext main loop exited.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in MetaContext runtime thread.");
            _startedTcs?.TrySetException(ex);
        }
        finally
        {
            _runtimeState.SetMetaContext(null, null);
            _logger.LogInformation("MetaContext removed from runtime state.");
            _lifetime.StopApplication();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping shell runtime service.");
        _runtimeState.SetShellReady(false);

        if (_runtimeThread is not null && _runtimeThread.IsAlive)
        {
            // We currently rely on MetaContext loop shutdown path; if it does not exit,
            // host shutdown timeout will enforce process termination.
            while (_runtimeThread.IsAlive && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);
            }
        }
    }
}
