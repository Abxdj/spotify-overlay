using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SpotifyAPI.Web;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace SpotifyOverlay;

public partial class OverlayWindow : Window
{
    private SpotifyClient? _spotify;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private string? _lastTrackId;
    private AudioAnalyzer? _audio;
    private Rectangle[] _bars = Array.Empty<Rectangle>();
    private const int BarCount = 16;

    public OverlayWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _spotify = await SpotifyAuth.GetClientAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Auth failed");
            Application.Current.Shutdown();
            return;
        }

        _timer.Tick += async (_, _) => await Refresh();
        _timer.Start();
        await Refresh();

        _audio = new AudioAnalyzer(BarCount);
        _audio.Start();
        BuildBars();

        var render = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        render.Tick += (_, _) => DrawBars();
        render.Start();
    }

    private void BuildBars()
    {
        VizCanvas.Children.Clear();
        _bars = new Rectangle[BarCount];
        for (int i = 0; i < BarCount; i++)
        {
            var r = new Rectangle
            {
                Width = 4,
                Fill = new SolidColorBrush(Color.FromRgb(0x1D, 0xB9, 0x54)), // spotify green
                RadiusX = 2, RadiusY = 2
            };
            _bars[i] = r;
            VizCanvas.Children.Add(r);
        }
    }

    private void DrawBars()
    {
        if (_audio == null || _bars.Length == 0) return;

        double totalW = VizCanvas.ActualWidth;
        if (totalW <= 0) return;

        double gap = 2;
        double barW = (totalW - gap * (BarCount - 1)) / BarCount;
        double maxH = VizCanvas.ActualHeight;

        for (int i = 0; i < BarCount; i++)
        {
            double h = Math.Max(2, _audio.Bands[i] * maxH);
            var r = _bars[i];
            r.Width = barW;
            r.Height = h;
            Canvas.SetLeft(r, i * (barW + gap));
            Canvas.SetTop(r, maxH - h); // grow upward from bottom
        }
    }

    private async Task Refresh()
    {
        if (_spotify == null) return;
        try
        {
            var playback = await _spotify.Player.GetCurrentPlayback();
            if (playback?.Item is FullTrack track)
            {
                TrackName.Text = track.Name;
                ArtistName.Text = string.Join(", ", track.Artists.Select(a => a.Name));

                if (track.Id != _lastTrackId)
                {
                    _lastTrackId = track.Id;
                    var url = track.Album.Images.FirstOrDefault()?.Url;
                    if (url != null)
                        AlbumArt.Source = new BitmapImage(new Uri(url));
                }
            }
            else
            {
                TrackName.Text = "Nothing playing";
                ArtistName.Text = "";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Refresh failed");
        }
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        if (_spotify == null) return;
        var pb = await _spotify.Player.GetCurrentPlayback();
        if (pb?.IsPlaying == true) await _spotify.Player.PausePlayback();
        else await _spotify.Player.ResumePlayback();
    }

    private async void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_spotify != null) { await _spotify.Player.SkipNext(); await Task.Delay(300); await Refresh(); }
    }

    private async void Prev_Click(object sender, RoutedEventArgs e)
    {
        if (_spotify != null) { await _spotify.Player.SkipPrevious(); await Task.Delay(300); await Refresh(); }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _audio?.Dispose();
        Application.Current.Shutdown();
    }

    // Lets you drag the widget anywhere on screen
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
}