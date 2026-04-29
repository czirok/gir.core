using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using GirCoreLauncher.Services;
using Microsoft.Extensions.Logging;

namespace GirCoreLauncher;

[UnsupportedOSPlatform("Windows")]
[UnsupportedOSPlatform("macOS")]
public sealed class GirCoreBar(Clutter.Stage stage, GirCoreLauncherCatalog appCatalog, ILogger<GirCoreBar> logger)
{
    private const int MaxLauncherApps = 14;
    private St.BoxLayout? _panel;

    private static readonly List<string> _barApps = new()
    {
        "org.gnome.Ptyxis.desktop",
        "org.gnome.Nautilus.desktop",
        "org.gnome.Settings.desktop",
        "org.gnome.Software.desktop",
        "org.gnome.Extensions.desktop",
        "org.gnome.tweaks.desktop",
        "org.gnome.Epiphany.desktop",
        "org.gnome.Weather.desktop",
        "org.gnome.font-viewer.desktop",
        "org.manjaro.pamac.manager.desktop",
    };

    public void EnsureVisible()
    {
        try
        {
            if (_panel is not null)
            {
                logger.LogInformation("[GirCoreLauncher.GirCoreBar] Launcher already exists; refreshing app buttons.");
                RebuildPanel(_panel);
                return;
            }

            _panel = St.BoxLayout.New();
            _panel.SetName("GirCoreBar");
            _panel.SetVertical(false);
            _panel.SetReactive(true);
            _panel.SetPosition(0, 0);
            _panel.SetSize(1400, 54);
            _panel.SetStyle(
                "background-color: rgba(18, 18, 22, 0.82); " +
                "padding: 7px 9px; spacing: 6px; color: white;");

            RebuildPanel(_panel);
            stage.AddChild(_panel);
            _panel.Show();
            _panel.QueueRedraw();
            stage.QueueRedraw();

            logger.LogInformation("[GirCoreLauncher.GirCoreBar] Mini launcher inserted on stage.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GirCoreLauncher.GirCoreBar] Failed to create mini launcher.");
        }
    }

    private void RebuildPanel(St.BoxLayout panel)
    {
        ClearChildren(panel);

        var apps = appCatalog.GetApps();
        foreach (var scannedApp in apps)
            logger.LogInformation("[AppCatalog.Launch] Scanned app. id={AppId} name={Name} file={DesktopFile}", scannedApp.Id, scannedApp.Name, scannedApp.DesktopFile);

        logger.LogInformation("[GirCoreLauncher.GirCoreBar] Building launcher buttons. visible_apps={VisibleApps} max_buttons={MaxButtons}", apps.Length, MaxLauncherApps);

        var title = St.Label.New("GirCore Bar");
        title.SetStyle("font-weight: bold; font-size: 13px; padding: 8px 12px 0 6px;");
        panel.AddChild(title);

        var added = 0;
        foreach (var app in apps)
        {
            if (_barApps.Count > 0 && !_barApps.Contains(app.Id))
                continue;

            if (added >= MaxLauncherApps)
                break;

            var button = St.Button.NewWithLabel(Shorten(app.Name, 24));
            button.SetName($"GirCoreShellLauncherButton-{SanitizeName(app.Id)}");
            button.SetReactive(true);
            button.SetCanFocus(true);
            button.SetTrackHover(true);
            button.SetStyle(
                "background-color: rgba(255,255,255,0.13); " +
                "border-radius: 6px; padding: 7px 10px; margin: 0 3px; color: white;");

            var appId = app.Id;
            button.OnClicked += (_, args) =>
            {
                logger.LogInformation("[GirCoreLauncher.GirCoreBar] Launcher button clicked. id={AppId} button={Button}", appId, args.ClickedButton);
                appCatalog.Launch(appId);
            };

            panel.AddChild(button);
            button.Show();
            added++;
        }

        logger.LogInformation("[GirCoreLauncher.GirCoreBar] Launcher build complete. buttons={ButtonCount}", added);
    }

    private static void ClearChildren(Clutter.Actor actor)
    {
        while (actor.GetFirstChild() is { } child)
            child.Destroy();
    }

    private static string Shorten(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;

        return value[..Math.Max(1, maxLength - 1)] + ".";
    }

    private static string SanitizeName(string value)
    {
        Span<char> buffer = stackalloc char[Math.Min(value.Length, 80)];
        var index = 0;
        foreach (var ch in value)
        {
            if (index >= buffer.Length)
                break;

            buffer[index++] = char.IsLetterOrDigit(ch) ? ch : '-';
        }

        return new string(buffer[..index]);
    }
}
