using System.Windows;

namespace SpotifyOverlay;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        new OverlayWindow().Show();
    }
}   