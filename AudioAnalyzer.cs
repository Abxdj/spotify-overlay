using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;

namespace SpotifyOverlay;

/// <summary>
/// Captures system audio via WASAPI loopback, runs an FFT, and exposes
/// a small number of frequency-band magnitudes for a bar visualizer.
/// </summary>
public class AudioAnalyzer : IDisposable
{
    private const int FftLength = 1024;              // must be power of 2
    private readonly int _bars;
    private readonly Complex[] _fftBuffer = new Complex[FftLength];
    private readonly float[] _sampleBuffer = new float[FftLength];
    private int _sampleIndex;

    private WasapiLoopbackCapture? _capture;

    /// <summary>Latest band magnitudes, 0..1, length = bars.</summary>
    public float[] Bands { get; }

    public AudioAnalyzer(int bars = 24)
    {
        _bars = bars;
        Bands = new float[bars];
    }

    public void Start()
    {
        _capture = new WasapiLoopbackCapture();       // default output device
        _capture.DataAvailable += OnData;
        _capture.StartRecording();
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        // Loopback is 32-bit IEEE float samples
        int bytesPerSample = 4;
        int channels = _capture!.WaveFormat.Channels;

        for (int i = 0; i + bytesPerSample * channels <= e.BytesRecorded; i += bytesPerSample * channels)
        {
            // average channels down to mono
            float sample = 0f;
            for (int ch = 0; ch < channels; ch++)
                sample += BitConverter.ToSingle(e.Buffer, i + ch * bytesPerSample);
            sample /= channels;

            _sampleBuffer[_sampleIndex++] = sample;

            if (_sampleIndex >= FftLength)
            {
                _sampleIndex = 0;
                ComputeFft();
            }
        }
    }

    private void ComputeFft()
    {
        for (int i = 0; i < FftLength; i++)
        {
            // Hann window to reduce spectral leakage
            float w = (float)(0.5 * (1 - Math.Cos(2 * Math.PI * i / (FftLength - 1))));
            _fftBuffer[i].X = _sampleBuffer[i] * w;
            _fftBuffer[i].Y = 0;
        }

        FastFourierTransform.FFT(true, (int)Math.Log2(FftLength), _fftBuffer);

        // Use first half of the spectrum, group into log-spaced bands
        int usableBins = FftLength / 2;
        for (int b = 0; b < _bars; b++)
        {
            // log scale so bass doesn't dominate visually
            int lo = (int)(Math.Pow((double)b / _bars, 2) * usableBins);
            int hi = (int)(Math.Pow((double)(b + 1) / _bars, 2) * usableBins);
            hi = Math.Max(hi, lo + 1);

            double sum = 0;
            for (int i = lo; i < hi && i < usableBins; i++)
            {
                double mag = Math.Sqrt(_fftBuffer[i].X * _fftBuffer[i].X +
                                       _fftBuffer[i].Y * _fftBuffer[i].Y);
                sum += mag;
            }
            double avg = sum / (hi - lo);

            // scale + clamp to 0..1 (tweak 15 to taste)
            float v = (float)Math.Min(1.0, avg * 15);
            // smooth: ease toward new value so bars don't jitter
            Bands[b] = Bands[b] * 0.6f + v * 0.4f;
        }
    }

    public void Dispose()
    {
        if (_capture != null)
        {
            _capture.DataAvailable -= OnData;
            _capture.StopRecording();
            _capture.Dispose();
            _capture = null;
        }
    }
}