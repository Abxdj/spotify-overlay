using System.Threading;
using System.Windows;

namespace SpotifyOverlay;

public partial class App : Application
{
    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, "SpotifyOverlay_SingleInstance", out bool isNew);
        if (!isNew)
        {
            Shutdown();   // another copy is already running
            return;
        }

        new OverlayWindow().Show();
    }
}